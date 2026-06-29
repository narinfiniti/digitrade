using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ApiGateway.Bff.IntegrationTests;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "xUnit disposes the fixture through IAsyncLifetime.")]
public sealed class GatewayBffIntegrationTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(180);
    private static readonly TimeSpan WebSocketReceiveTimeout = TimeSpan.FromSeconds(15);
    private static readonly string TradeConnectionString = "Host=127.0.0.1;Port=55432;Database=digitrade_trade_phase2_tests;Username=postgres;Password=postgres";
    private static readonly string OrchestratorConnectionString = "Host=127.0.0.1;Port=55432;Database=postgres;Username=postgres;Password=postgres";

    private readonly List<HostedProcess> hostedProcesses = [];
    private HttpClient? gatewayClient;
    private HttpClient? identityClient;
    private HostedContainer? kongContainer;
    private HostedPostgresContainer? postgresContainer;
    private string repoRoot = string.Empty;

    public async Task InitializeAsync()
    {
        repoRoot = ResolveRepoRoot();

        postgresContainer = new HostedPostgresContainer(repoRoot, "digitrade-phase2-postgres-tests", 55432);
        await postgresContainer.StartAsync();
        await postgresContainer.WaitUntilReadyAsync();

        await EnsureOrchestratorPersistenceReadyAsync(repoRoot);

        identityClient = CreateHttpClient("http://127.0.0.1:5012");
        gatewayClient = CreateHttpClient("http://127.0.0.1:5023");

        hostedProcesses.Add(new HostedProcess("IdentityService", repoRoot, "src/Identity/Identity.Api/Identity.Api.csproj", 5012));
        hostedProcesses.Add(new HostedProcess("AccountService", repoRoot, "src/Account/Account.Api/Account.Api.csproj", 5011));
        hostedProcesses.Add(new HostedProcess("InstrumentService", repoRoot, "src/Instrument/Instrument.Api/Instrument.Api.csproj", 5013));
        hostedProcesses.Add(new HostedProcess(
            "TradeService",
            repoRoot,
            "src/Trade/Trade.Api/Trade.Api.csproj",
            5014,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["TRADE_DB_CONNECTION"] = TradeConnectionString,
            }));
        hostedProcesses.Add(new HostedProcess("OrderService", repoRoot, "src/Order/Order.Api/Order.Api.csproj", 5015));
        hostedProcesses.Add(new HostedProcess("BffAggregatorService", repoRoot, "src/BffAggregatorService/BffAggregatorService.Api/BffAggregatorService.Api.csproj", 5024));
        hostedProcesses.Add(new HostedProcess(
            "BffOrchestratorService",
            repoRoot,
            "src/BffOrchestratorService/BffOrchestratorService.Api/BffOrchestratorService.Api.csproj",
            5025,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["BFF_ORCHESTRATOR_DB_CONNECTION"] = OrchestratorConnectionString,
            }));
        hostedProcesses.Add(new HostedProcess("BffNotificationService", repoRoot, "src/BffNotificationService/BffNotificationService.Api/BffNotificationService.Api.csproj", 5026));

        foreach (var hostedProcess in hostedProcesses)
        {
            await hostedProcess.StartAsync();
        }

        kongContainer = new HostedContainer(repoRoot, "digitrade-phase2-gateway-tests", 5023);
        await kongContainer.StartAsync();

        await WaitForHealthyAsync(gatewayClient!, "/health/ready", StartupTimeout, () => kongContainer.GetLogsAsync());
    }

    public async Task DisposeAsync()
    {
        if (kongContainer is not null)
        {
            await kongContainer.DisposeAsync();
        }

        if (postgresContainer is not null)
        {
            await postgresContainer.DisposeAsync();
        }

        foreach (var hostedProcess in hostedProcesses.AsEnumerable().Reverse())
        {
            await hostedProcess.DisposeAsync();
        }

        gatewayClient?.Dispose();
        identityClient?.Dispose();
    }

    [Fact]
    public async Task GatewayHealthEndpointsReturnOk()
    {
        using var liveResponse = await gatewayClient!.GetAsync("/health/live");
        using var readyResponse = await gatewayClient.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readyResponse.StatusCode);
        Assert.Equal("Healthy", await liveResponse.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", await readyResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GatewayRejectsUnauthenticatedReadRequests()
    {
        using var response = await gatewayClient!.GetAsync("/api/v1/read/aggregations/services/health-summary");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        using var document = await ReadJsonDocumentAsync(response);
        Assert.Equal("gateway.authentication.missing_bearer_token", document.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task GatewayRoutesAuthenticatedReadRequestsToAggregator()
    {
        var identity = await CreateAuthenticatedIdentityAsync();
        using var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/v1/read/aggregations/services/health-summary", identity.AccessToken, Guid.NewGuid().ToString("N"));
        using var response = await gatewayClient!.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Correlation-Id"));

        using var document = await ReadJsonDocumentAsync(response);
        Assert.True(document.RootElement.GetProperty("isHealthy").GetBoolean());

        var serviceNames = document.RootElement
            .GetProperty("services")
            .EnumerateArray()
            .Select(service => service.GetProperty("serviceName").GetString())
            .OfType<string>()
            .ToArray();

        Assert.Contains("AccountService", serviceNames);
        Assert.Contains("InstrumentService", serviceNames);
    }

    [Fact]
    public async Task GatewayRoutesAuthenticatedSynchronousWriteRequestsToOrchestrator()
    {
        var identity = await CreateAuthenticatedIdentityAsync();
        var correlationId = Guid.NewGuid().ToString("N");

        using var createRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/write/orchestrations/requests/", identity.AccessToken, correlationId);
        createRequest.Content = JsonContent.Create(new CreateOrchestrationShellRequest("phase2-orchestration"));

        using var createResponse = await gatewayClient!.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        Assert.True(createResponse.Headers.Contains("X-Correlation-Id"));

        using var createDocument = await ReadJsonDocumentAsync(createResponse);
        var orchestrationShellId = createDocument.RootElement.GetProperty("orchestrationShellId").GetGuid();
        Assert.Equal("Accepted", createDocument.RootElement.GetProperty("status").GetString());
        Assert.Equal(identity.UserId.ToString(), createDocument.RootElement.GetProperty("requestedBySubjectId").GetString());
        Assert.Equal(identity.UserName, createDocument.RootElement.GetProperty("requestedByUserName").GetString());
        Assert.True(createDocument.RootElement.GetProperty("dependenciesHealthy").GetBoolean());

        using var getRequest = CreateAuthenticatedRequest(HttpMethod.Get, $"/api/v1/write/orchestrations/requests/{orchestrationShellId}", identity.AccessToken, correlationId);
        using var getResponse = await gatewayClient.SendAsync(getRequest);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var getDocument = await ReadJsonDocumentAsync(getResponse);
        Assert.Equal(orchestrationShellId, getDocument.RootElement.GetProperty("orchestrationShellId").GetGuid());
        Assert.Equal(identity.UserId.ToString(), getDocument.RootElement.GetProperty("requestedBySubjectId").GetString());
        Assert.Equal(identity.UserName, getDocument.RootElement.GetProperty("requestedByUserName").GetString());
    }

    [Fact]
    public async Task GatewayReturnsServiceUnavailableWhenSynchronousDependenciesUnavailable()
    {
        var identity = await CreateAuthenticatedIdentityAsync();
        var correlationId = Guid.NewGuid().ToString("N");
        var flowName = $"phase9-sync-degraded-{Guid.NewGuid():N}";
        var orchestratorServiceProcess = GetHostedProcess("BffOrchestratorService");

        await orchestratorServiceProcess.SetEnvironmentVariableAsync("OrchestratorDownstreamServices__TradeServiceBaseUrl", "http://127.0.0.1:6514");
        await orchestratorServiceProcess.SetEnvironmentVariableAsync("OrchestratorDownstreamServices__OrderServiceBaseUrl", "http://127.0.0.1:6515");
        await orchestratorServiceProcess.RestartAsync();

        try
        {
            using var createRequest = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/write/orchestrations/requests/", identity.AccessToken, correlationId);
            createRequest.Content = JsonContent.Create(new CreateOrchestrationShellRequest(flowName));

            using var createResponse = await gatewayClient!.SendAsync(createRequest);

            Assert.Equal(HttpStatusCode.ServiceUnavailable, createResponse.StatusCode);
            Assert.True(createResponse.Headers.Contains("X-Correlation-Id"));

            using var createDocument = await ReadJsonDocumentAsync(createResponse);
            Assert.Equal("PendingDependencies", createDocument.RootElement.GetProperty("status").GetString());
            Assert.False(createDocument.RootElement.GetProperty("dependenciesHealthy").GetBoolean());

            var dependencies = createDocument.RootElement.GetProperty("dependencies").EnumerateArray().ToArray();
            Assert.Contains(
                dependencies,
                dependency =>
                    (string.Equals(dependency.GetProperty("serviceName").GetString(), "OrderService", StringComparison.Ordinal)
                        || string.Equals(dependency.GetProperty("serviceName").GetString(), "TradeService", StringComparison.Ordinal))
                    && !dependency.GetProperty("isHealthy").GetBoolean());
        }
        finally
        {
            await orchestratorServiceProcess.SetEnvironmentVariableAsync("OrchestratorDownstreamServices__TradeServiceBaseUrl", "http://127.0.0.1:5014");
            await orchestratorServiceProcess.SetEnvironmentVariableAsync("OrchestratorDownstreamServices__OrderServiceBaseUrl", "http://127.0.0.1:5015");
            await orchestratorServiceProcess.RestartAsync();
        }
    }

    [Fact]
    public async Task GatewayRoutesAuthenticatedWebSocketNotifications()
    {
        var identity = await CreateAuthenticatedIdentityAsync();
        using var webSocket = new ClientWebSocket();
        webSocket.Options.SetRequestHeader("Authorization", $"Bearer {identity.AccessToken}");

        await webSocket.ConnectAsync(CreateGatewayWebSocketUri(), CancellationToken.None);

        var correlationId = Guid.NewGuid().ToString("N");
        using var request = CreateAuthenticatedRequest(HttpMethod.Post, "/api/v1/notifications/terminal-completions", identity.AccessToken, correlationId);
        request.Content = JsonContent.Create(
            new TerminalNotificationRequest(
                "phase2-aggregate",
                identity.UserId.ToString(),
                "websocket",
                "Phase 2 notification",
                "Gateway websocket notification routed successfully."));

        using var response = await gatewayClient!.SendAsync(request);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        using var messageDocument = await ReceiveWebSocketEnvelopeAsync(webSocket, WebSocketReceiveTimeout);
        Assert.Equal(identity.UserId.ToString(), messageDocument.RootElement.GetProperty("recipientId").GetString());
        Assert.Equal("websocket", messageDocument.RootElement.GetProperty("channel").GetString());
        Assert.Equal("Phase 2 notification", messageDocument.RootElement.GetProperty("subject").GetString());
        Assert.Equal("Gateway websocket notification routed successfully.", messageDocument.RootElement.GetProperty("message").GetString());
        Assert.Equal(correlationId, messageDocument.RootElement.GetProperty("correlationId").GetString());

        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete.", CancellationToken.None);
    }

    private async Task<AuthenticatedIdentity> CreateAuthenticatedIdentityAsync()
    {
        var uniqueSuffix = Guid.NewGuid().ToString("N");
        var userName = $"phase2-gateway-user-{uniqueSuffix}";
        var email = $"phase2-gateway-user-{uniqueSuffix}@example.test";
        const string password = "Phase2GatewayPass!123";

        using var registerResponse = await identityClient!.PostAsJsonAsync(
            "/api/v1/identity/users",
            new RegisterUserRequest(userName, email, password));

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        using var registerDocument = await ReadJsonDocumentAsync(registerResponse);
        var userId = registerDocument.RootElement.GetProperty("userId").GetGuid();

        using var tokenResponse = await identityClient!.PostAsJsonAsync(
            "/api/v1/identity/tokens",
            new IssueAccessTokenRequest(userName, password));

        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);

        using var tokenDocument = await ReadJsonDocumentAsync(tokenResponse);
        var accessToken = tokenDocument.RootElement.GetProperty("accessToken").GetString();

        Assert.False(string.IsNullOrWhiteSpace(accessToken));

        return new AuthenticatedIdentity(userId, userName, accessToken!);
    }

    private static HttpClient CreateHttpClient(string baseAddress)
    {
        return new HttpClient
        {
            BaseAddress = new Uri(baseAddress, UriKind.Absolute),
            Timeout = TimeSpan.FromSeconds(30),
        };
    }

    private static HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string path, string accessToken, string correlationId)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
        request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);
        return request;
    }

    private static async Task<JsonDocument> ReadJsonDocumentAsync(HttpResponseMessage response)
    {
        await using var contentStream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(contentStream);
    }

    private static async Task<JsonDocument> ReceiveWebSocketEnvelopeAsync(ClientWebSocket webSocket, TimeSpan timeout)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var messageBuffer = new MemoryStream();
        var receiveBuffer = new byte[4 * 1024];

        while (true)
        {
            var receiveResult = await webSocket.ReceiveAsync(receiveBuffer, timeoutCts.Token);

            if (receiveResult.MessageType == WebSocketMessageType.Close)
            {
                throw new InvalidOperationException("The notification stream closed before a message was received.");
            }

            messageBuffer.Write(receiveBuffer, 0, receiveResult.Count);

            if (receiveResult.EndOfMessage)
            {
                break;
            }
        }

        messageBuffer.Position = 0;
        return await JsonDocument.ParseAsync(messageBuffer, cancellationToken: timeoutCts.Token);
    }

    private static async Task WaitForHealthyAsync(
        HttpClient client,
        string healthPath,
        TimeSpan timeout,
        Func<Task<string>>? logProvider = null)
    {
        var startedAt = Stopwatch.GetTimestamp();

        while (ElapsedSince(startedAt) < timeout)
        {
            try
            {
                using var response = await client.GetAsync(healthPath);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }

            await Task.Delay(500);
        }

        var failureMessage = $"Timed out waiting for '{client.BaseAddress}{healthPath}' to become healthy within {timeout}.";
        if (logProvider is not null)
        {
            failureMessage = $"{failureMessage}{Environment.NewLine}{await logProvider()}";
        }

        throw new TimeoutException(failureMessage);
    }

    private static TimeSpan ElapsedSince(long startedAtTimestamp)
    {
        var elapsedTicks = Stopwatch.GetTimestamp() - startedAtTimestamp;
        return TimeSpan.FromSeconds(elapsedTicks / (double)Stopwatch.Frequency);
    }

    private static Uri CreateGatewayWebSocketUri()
    {
        return new Uri("ws://127.0.0.1:5023/api/v1/notifications/stream", UriKind.Absolute);
    }

    private static string ResolveRepoRoot()
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        while (currentDirectory is not null)
        {
            if (File.Exists(Path.Combine(currentDirectory.FullName, "DigiTrade.sln")))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException("Could not locate the DigiTrade solution root from the current test output directory.");
    }

    private HostedProcess GetHostedProcess(string processName)
    {
        return hostedProcesses.Single(process => string.Equals(process.Name, processName, StringComparison.Ordinal));
    }

    private static async Task EnsureOrchestratorPersistenceReadyAsync(string solutionRoot)
    {
        await RunProcessAsync(
            solutionRoot,
            "dotnet",
            [
                "ef",
                "database",
                "update",
                "--project",
                "src/BffOrchestratorService/BffOrchestratorService.Persistence/BffOrchestratorService.Persistence.csproj",
                "--startup-project",
                "src/BffOrchestratorService/BffOrchestratorService.Api/BffOrchestratorService.Api.csproj",
                "--context",
                "BffOrchestratorDbContext",
            ],
            ignoreExitCode: false,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["BFF_ORCHESTRATOR_DB_CONNECTION"] = OrchestratorConnectionString,
            });
    }

    private sealed record AuthenticatedIdentity(Guid UserId, string UserName, string AccessToken);

    private sealed record RegisterUserRequest(string UserName, string Email, string Password);

    private sealed record IssueAccessTokenRequest(string Login, string Password);

    private sealed record CreateOrchestrationShellRequest(string FlowName);

    private sealed record TerminalNotificationRequest(
        string AggregateId,
        string RecipientId,
        string Channel,
        string Subject,
        string Message);

    private sealed class HostedProcess(
        string name,
        string repoRoot,
        string projectPath,
        int port,
        IDictionary<string, string>? environmentVariables = null)
        : IAsyncDisposable
    {
        private readonly IDictionary<string, string> environmentVariables = environmentVariables ?? new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly StringBuilder output = new();
        private Process? process;

        public string Name => name;

        public async Task StartAsync()
        {
            if (process is not null)
            {
                if (!process.HasExited)
                {
                    throw new InvalidOperationException($"{name} is already running.");
                }

                process.Dispose();
                process = null;
            }

            var startInfo = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            startInfo.ArgumentList.Add("run");
            startInfo.ArgumentList.Add("--project");
            startInfo.ArgumentList.Add(projectPath);
            startInfo.ArgumentList.Add("--no-launch-profile");

            startInfo.Environment["ASPNETCORE_URLS"] = $"http://127.0.0.1:{port}";
            startInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";

            foreach (var environmentVariable in environmentVariables)
            {
                startInfo.Environment[environmentVariable.Key] = environmentVariable.Value;
            }

            process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            process.OutputDataReceived += (_, eventArgs) => AppendOutput(eventArgs.Data);
            process.ErrorDataReceived += (_, eventArgs) => AppendOutput(eventArgs.Data);

            if (!process.Start())
            {
                throw new InvalidOperationException($"Failed to start {name} from '{projectPath}'.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var httpClient = CreateHttpClient($"http://127.0.0.1:{port}");
            await WaitForHealthyAsync(httpClient, "/health/ready", StartupTimeout, GetLogsAsync);
        }

        public async Task StopAsync()
        {
            if (process is null)
            {
                return;
            }

            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }

                await process.WaitForExitAsync();
            }
            catch (InvalidOperationException)
            {
            }
            finally
            {
                process.Dispose();
                process = null;
            }
        }

        public Task SetEnvironmentVariableAsync(string key, string value)
        {
            environmentVariables[key] = value;
            return Task.CompletedTask;
        }

        public async Task RestartAsync()
        {
            await StopAsync();
            await StartAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync();
        }

        private void AppendOutput(string? line)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                output.AppendLine(line);
            }
        }

        private Task<string> GetLogsAsync()
        {
            var processStatus = process is not null && process.HasExited
                ? $"Process exited with code {process.ExitCode}."
                : "Process is still running.";

            return Task.FromResult($"{name} startup log:{Environment.NewLine}{processStatus}{Environment.NewLine}{output}");
        }
    }

    private sealed class HostedContainer(string repoRoot, string name, int hostPort) : IAsyncDisposable
    {
        public async Task StartAsync()
        {
            await RunDockerCommandAsync(["rm", "-f", name], ignoreExitCode: true);

            var kongConfigPath = ToDockerPath(Path.Combine(repoRoot, "kong", "kong.local.yml"));
            var kongPluginPath = ToDockerPath(Path.Combine(repoRoot, "kong", "plugins", "identity-introspection"));

            await RunDockerCommandAsync(
                "run",
                "--rm",
                "-d",
                "--name",
                name,
                "-p",
                $"{hostPort}:8000",
                "-e",
                "KONG_DATABASE=off",
                "-e",
                "KONG_DECLARATIVE_CONFIG=/usr/local/kong/declarative/kong.yml",
                "-e",
                "KONG_PROXY_LISTEN=0.0.0.0:8000",
                "-e",
                "KONG_PLUGINS=bundled,identity-introspection",
                "-v",
                $"{kongConfigPath}:/usr/local/kong/declarative/kong.yml:ro",
                "-v",
                $"{kongPluginPath}:/usr/local/share/lua/5.1/kong/plugins/identity-introspection:ro",
                "kong:3.7");
        }

        public async ValueTask DisposeAsync()
        {
            await RunDockerCommandAsync(["rm", "-f", name], ignoreExitCode: true);
        }

        public async Task<string> GetLogsAsync()
        {
            var (_, standardOutput, standardError) = await RunProcessAsync(
                repoRoot,
                "docker",
                ["logs", name],
                ignoreExitCode: true);

            return $"Kong container logs:{Environment.NewLine}{standardOutput}{Environment.NewLine}{standardError}";
        }

        private async Task RunDockerCommandAsync(params string[] arguments)
        {
            await RunDockerCommandAsync(arguments, ignoreExitCode: false);
        }

        private async Task RunDockerCommandAsync(string[] arguments, bool ignoreExitCode)
        {
            await RunProcessAsync(repoRoot, "docker", arguments, ignoreExitCode);
        }

        private static string ToDockerPath(string path)
        {
            return path.Replace('\\', '/');
        }
    }

    private sealed class HostedPostgresContainer(string repoRoot, string name, int hostPort) : IAsyncDisposable
    {
        private static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(60);

        public async Task StartAsync()
        {
            await RunDockerCommandAsync(["rm", "-f", name], ignoreExitCode: true);

            await RunDockerCommandAsync(
                "run",
                "--rm",
                "-d",
                "--name",
                name,
                "-p",
                $"{hostPort}:5432",
                "-e",
                "POSTGRES_USER=postgres",
                "-e",
                "POSTGRES_PASSWORD=postgres",
                "-e",
                "POSTGRES_DB=postgres",
                "postgres:16-alpine");
        }

        public async Task WaitUntilReadyAsync()
        {
            var startedAt = Stopwatch.GetTimestamp();

            while (ElapsedSince(startedAt) < ReadyTimeout)
            {
                var (exitCode, _, _) = await RunProcessAsync(
                    repoRoot,
                    "docker",
                    ["exec", name, "pg_isready", "-U", "postgres", "-d", "postgres"],
                    ignoreExitCode: true);

                if (exitCode == 0)
                {
                    return;
                }

                await Task.Delay(500);
            }

            var logs = await GetLogsAsync();
            throw new TimeoutException($"Timed out waiting for PostgreSQL container '{name}' to become ready.{Environment.NewLine}{logs}");
        }

        public async ValueTask DisposeAsync()
        {
            await RunDockerCommandAsync(["rm", "-f", name], ignoreExitCode: true);
        }

        private async Task<string> GetLogsAsync()
        {
            var (_, standardOutput, standardError) = await RunProcessAsync(
                repoRoot,
                "docker",
                ["logs", name],
                ignoreExitCode: true);

            return $"PostgreSQL container logs:{Environment.NewLine}{standardOutput}{Environment.NewLine}{standardError}";
        }

        private async Task RunDockerCommandAsync(string[] arguments, bool ignoreExitCode)
        {
            await RunProcessAsync(repoRoot, "docker", arguments, ignoreExitCode);
        }

        private async Task RunDockerCommandAsync(params string[] arguments)
        {
            await RunDockerCommandAsync(arguments, ignoreExitCode: false);
        }
    }

    private static async Task<(int ExitCode, string StandardOutput, string StandardError)> RunProcessAsync(
        string workingDirectory,
        string fileName,
        IEnumerable<string> arguments,
        bool ignoreExitCode,
        IReadOnlyDictionary<string, string>? environmentVariables = null)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (environmentVariables is not null)
        {
            foreach (var environmentVariable in environmentVariables)
            {
                startInfo.Environment[environmentVariable.Key] = environmentVariable.Value;
            }
        }

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException($"Failed to start process '{fileName}'.");
        }

        var standardOutput = await process.StandardOutput.ReadToEndAsync();
        var standardError = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (!ignoreExitCode && process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Process '{fileName}' exited with code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{standardOutput}{Environment.NewLine}stderr:{Environment.NewLine}{standardError}");
        }

        return (process.ExitCode, standardOutput, standardError);
    }
}