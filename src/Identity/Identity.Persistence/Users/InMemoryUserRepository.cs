using Identity.Application.Abstractions;
using Identity.Domain.Users;

namespace Identity.Persistence.Users;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly object syncRoot = new();
    private readonly Dictionary<Guid, User> usersById = new();
    private readonly Dictionary<string, Guid> userIdsByUserName = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Guid> userIdsByEmail = new(StringComparer.Ordinal);

    public Task<bool> ExistsByUserNameAsync(string normalizedUserName, CancellationToken cancellationToken = default)
    {
        lock (syncRoot)
        {
            return Task.FromResult(userIdsByUserName.ContainsKey(normalizedUserName));
        }
    }

    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        lock (syncRoot)
        {
            return Task.FromResult(userIdsByEmail.ContainsKey(normalizedEmail));
        }
    }

    public Task<User?> FindByLoginAsync(string normalizedLogin, CancellationToken cancellationToken = default)
    {
        lock (syncRoot)
        {
            if (userIdsByUserName.TryGetValue(normalizedLogin, out var userId)
                || userIdsByEmail.TryGetValue(normalizedLogin, out userId))
            {
                usersById.TryGetValue(userId, out var user);
                return Task.FromResult(user);
            }

            return Task.FromResult<User?>(null);
        }
    }

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        lock (syncRoot)
        {
            if (userIdsByUserName.ContainsKey(user.NormalizedUserName)
                || userIdsByEmail.ContainsKey(user.NormalizedEmail))
            {
                throw new InvalidOperationException("The user already exists.");
            }

            usersById[user.Id] = user;
            userIdsByUserName[user.NormalizedUserName] = user.Id;
            userIdsByEmail[user.NormalizedEmail] = user.Id;
        }

        return Task.CompletedTask;
    }
}