using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Web;

namespace DigiTrade.Common.Extensions;

/// <summary>
/// StringExtensions
/// </summary>
public static class StringExtensions
{
    [DebuggerStepThrough]
    public static string ToSpaceSeparatedString(this IEnumerable<string>? list)
    {
        if(list == null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder(100);

        foreach (var element in list)
        {
            sb.Append(element + " ");
        }

        return sb.ToString().Trim();
    }

    [DebuggerStepThrough]
    public static IEnumerable<string> FromSpaceSeparatedString(this string input)
    {
        input = input.Trim();
        return input.Split(separator, StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    [DebuggerStepThrough]
    public static bool IsEmpty(this string str)
    {
        return string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str);
    }

    [DebuggerStepThrough]
    public static bool IsNotEmpty(this string str)
    {
        return !str.IsEmpty();
    }

    [DebuggerStepThrough]
    public static string HasValue(this string str, string result)
    {
        return !str.IsEmpty() ? result : string.Empty;
    }

    public static string HasValue<T>(this T? str, string result) where T : struct
    {
        return str.HasValue ? result : string.Empty;
    }

    public static List<string>? ParseScopesString(this string scopes)
    {
        if(scopes.IsEmpty())
        {
            return null;
        }

        scopes = scopes.Trim();
        var parsedScopes = scopes.Split(separator, StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();

        if(parsedScopes.Count != 0)
        {
            parsedScopes.Sort();
            return parsedScopes;
        }

        return null;
    }

    [DebuggerStepThrough]
    public static string ReplaceRecursive(this string value, string pattern, string val = "")
    {
        while (value.Contains(pattern))
        {
            value = value.Replace(pattern, val);
        }

        return value;
    }


    [DebuggerStepThrough]
    public static bool IsMissingOrTooLong(this string value, int maxLength)
    {
        if(string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if(value.Length > maxLength)
        {
            return true;
        }

        return false;
    }


    [DebuggerStepThrough]
    public static string EnsureLeadingSlash(this string url)
    {
        if(url != null && !url.StartsWith('/'))
        {
            return "/" + url;
        }

        return url ?? string.Empty;
    }

    [DebuggerStepThrough]
    public static string EnsureTrailingSlash(this string url)
    {
        if(url != null && !url.EndsWith('/'))
        {
            return url + "/";
        }

        return url ?? string.Empty;
    }

    [DebuggerStepThrough]
    public static string RemoveLeadingSlash(this string url)
    {
        if(url != null && url.StartsWith('/'))
        {
            url = url.Substring(1);
        }

        return url ?? string.Empty;
    }

    [DebuggerStepThrough]
    public static string RemoveTrailingSlash(this string url)
    {
        if(url != null && url.EndsWith('/'))
        {
            url = url.Substring(0, url.Length - 1);
        }

        return url ?? string.Empty;
    }

    [DebuggerStepThrough]
    public static string CleanUrlPath(this string url)
    {
        if(string.IsNullOrWhiteSpace(url))
            url = "/";

        if(url != null && url != "/" && url.EndsWith('/'))
        {
            url = url.Substring(0, url.Length - 1);
        }

        return url ?? "/";
    }

    [DebuggerStepThrough]
    public static bool IsLocalUrl(this string url)
    {
        if(string.IsNullOrEmpty(url))
        {
            return false;
        }

        // Allows "/" or "/foo" but not "//" or "/\".
        if(url[0] == '/')
        {
            // url is exactly "/"
            if(url.Length == 1)
            {
                return true;
            }

            // url doesn't start with "//" or "/\"
            if(url[1] != '/' && url[1] != '\\')
            {
                return true;
            }

            return false;
        }

        // Allows "~/" or "~/foo" but not "~//" or "~/\".
        if(url[0] == '~' && url.Length > 1 && url[1] == '/')
        {
            // url is exactly "~/"
            if(url.Length == 2)
            {
                return true;
            }

            // url doesn't start with "~//" or "~/\"
            if(url[2] != '/' && url[2] != '\\')
            {
                return true;
            }

            return false;
        }

        return false;
    }

    [DebuggerStepThrough]
    public static string AddQueryString(this string url, string query)
    {
        if(!url.Contains('?'))
        {
            url += "?";
        }
        else if(!url.EndsWith('&'))
        {
            url += "&";
        }

        return url + query;
    }

    [DebuggerStepThrough]
    public static string AddQueryString(this string url, string name, string value)
    {
        return url.AddQueryString(name + "=" + UrlEncoder.Default.Encode(value));
    }

    [DebuggerStepThrough]
    public static string ToQueryString<T>(this T ob, bool lowerCase = true)
    {
        if (ob == null) return string.Empty;
        
        var nameValuePairs = from p in ob.GetType().GetProperties()
            where p.GetValue(ob, null) != null
            select $"{p.Name}={HttpUtility.UrlEncode(p.GetValue(ob, null)?.ToString() ?? string.Empty)}";
        var result = $"?{string.Join("&", nameValuePairs.ToArray())}";
        return lowerCase ? result.ToLower(CultureInfo.InvariantCulture) : result;
    }

    [DebuggerStepThrough]
    public static string ToQueryString(this NameValueCollection nvc)
    {
        if(nvc == null) return string.Empty;

        var sb = new StringBuilder();

        foreach (string key in nvc.Keys)
        {
            if(string.IsNullOrWhiteSpace(key)) continue;

            var values = nvc.GetValues(key);
            if(values == null) continue;

            foreach (var value in values)
            {
                sb.Append(sb.Length == 0 ? "?" : "&");
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}", Uri.EscapeDataString(key), Uri.EscapeDataString(value));
            }
        }

        return sb.ToString();
    }

    [DebuggerStepThrough]
    public static string AddHashFragment(this string url, string query)
    {
        if(!url.Contains('#'))
        {
            url += "#";
        }

        return url + query;
    }

    public static string? GetOrigin(this string url)
    {
        if(url != null)
        {
            Uri uri;
            try
            {
                uri = new Uri(url);
            }
            catch (Exception)
            {
                return null;
            }

            if(uri.Scheme == "http" || uri.Scheme == "https")
            {
                return $"{uri.Scheme}://{uri.Authority}";
            }
        }

        return null;
    }

    public static string Replace(
        this string input, string regex = "[^'a-zA-Z0-9 -]", string replace = "")
    {
        var whiteList = new Regex(regex);
        if(!string.IsNullOrEmpty(input) && !string.IsNullOrWhiteSpace(input))
        {
            input = input.Trim();
            try
            {
                while (whiteList.IsMatch(input))
                {
                    input = whiteList.Replace(input, replace);
                }

                return input;
            }
            catch (Exception)
            {
                // ignored
            }
        }

        return string.Empty;
    }

    public static string AsOneLine(this string text, string separator = " ")
    {
        return new Regex(@"(?:\n(?:\s*))+").Replace(text, separator).Trim();
    }

    public static string FirstLower(this string str)
    {
        if(string.IsNullOrEmpty(str) || char.IsLower(str, 0))
        {
            return str;
        }

        return char.ToLowerInvariant(str[0]) + str.Substring(1);
    }

    public static string Capitalize(this string word)
    {
        return $"{word[0].ToString().ToUpper(System.Globalization.CultureInfo.InvariantCulture)}{word[1..]}";
    }

    public static string ToUpperEveryWord(this string s)
    {
        // Check for empty string.
        if(string.IsNullOrEmpty(s))
        {
            return string.Empty;
        }

        s = s.ToLowerInvariant();
        return char.ToUpper(s[0], System.Globalization.CultureInfo.InvariantCulture) + s.Substring(1);
    }

    public static bool ContainsAny(this string[] blackList, string name)
    {
        return !string.IsNullOrEmpty(name) && blackList.Any(name.ToUpperInvariant().Trim().Contains);
    }

    public static bool IsLike(this string s, string? val)
    {
        s = s.Replace(" ", "_");
        val = val?.Replace(" ", "_");
        return val is not null && s.Is(val);
    }

    public static bool Is(this string s, string val)
    {
        return s.Equals(val, StringComparison.OrdinalIgnoreCase);
    }

    public static bool Includes(this string s, string val)
    {
        return s.Contains(val, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsNot(this string s, string val)
    {
        return !s.Is(val);
    }

    private static readonly char[] SeparatorChars = [' ', '\t', '"', '\'', '\"'];
  private static readonly char[] separator = [' '];

  public static string RemoveTabsAndSpacesAndQuotes(this string input)
    {
        return string.Join("",
            input.Split(SeparatorChars, StringSplitOptions.RemoveEmptyEntries));
    }
}