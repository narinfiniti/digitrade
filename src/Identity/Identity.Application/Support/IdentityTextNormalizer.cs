namespace Identity.Application.Support;

internal static class IdentityTextNormalizer
{
    public static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();
    }
}