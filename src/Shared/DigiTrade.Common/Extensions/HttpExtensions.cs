using System.Reflection;
using System.Text;
using System.Text.Json;
using DigiTrade.Common.Exceptions;

namespace DigiTrade.Common.Extensions;

/// <summary>
/// HttpExtensions
/// </summary>
public static class HttpExtensions
{
    public static async Task<T> RequestAsync<T>(this HttpClient httpClient,
        HttpMethod method, string url, HttpContent? content = null,
        List<KeyValuePair<string, string>>? headers = null,
        JsonSerializerOptions? serializerOptions = null, CancellationToken cancel = default)
    {
        var httpReqMsg = new HttpRequestMessage(method, url);
        httpReqMsg.Headers.Clear();
        if(headers != null)
        {
            foreach (var kvp in headers)
            {
                httpReqMsg.Headers.Add(kvp.Key, kvp.Value);
            }
        }

        if(content != null)
        {
            httpReqMsg.Content = content;
        }

        // var retryPolicy = Policy
        //     .Handle<HttpRequestException>()
        //     .Or<TimeoutException>()
        //     .WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(2));

        // var httpResMsg = await retryPolicy.ExecuteAsync(() => httpClient.SendAsync(httpReqMsg, cancel));
        var httpResMsg = await httpClient.SendAsync(httpReqMsg, cancel);
        if(!httpResMsg.IsSuccessStatusCode)
        {
            var stringContent = await httpResMsg.Content.ReadAsStringAsync(cancel);
            throw new BadRequestException(httpResMsg.ReasonPhrase ?? "The server cannot process the request due to client error/s.", (int)httpResMsg.StatusCode);
        }

        await using var stream = await httpResMsg.Content.ReadAsStreamAsync(cancel);
        return (await JsonSerializer.DeserializeAsync<T>(stream, serializerOptions, cancel))!;
    }

    public static StringContent ToStringContent(this object value, JsonSerializerOptions? options = null)
    {
        return new StringContent(JsonSerializer.Serialize(value, options), Encoding.UTF8, "application/json");
    }

    public static FormUrlEncodedContent ToFormUrlEncodedContent(this object obj)
    {
        var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var keyValuePairs = (from property in properties
            let value = property.GetValue(obj)?.ToString()
            where value != null
            select new KeyValuePair<string, string>(property.Name, value)).ToList();

        return new FormUrlEncodedContent(keyValuePairs);
    }
}