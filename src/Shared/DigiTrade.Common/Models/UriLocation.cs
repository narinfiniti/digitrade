namespace DigiTrade.Common.Models;

/// <summary>
/// https://developer.mozilla.org/en-US/docs/Web/API/Location
/// </summary>
public class UriLocation
{
    public required string Host { get; set; }
    public required string Pathname { get; set; }
    public required string Scheme { get; set; }
    public string Href => $"{Scheme}://{Host}{Pathname}";
}