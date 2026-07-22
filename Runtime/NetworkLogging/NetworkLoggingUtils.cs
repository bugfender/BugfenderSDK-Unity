using System;
using System.Collections.Generic;
using System.Text;

internal static class NetworkLoggingUtils
{
    private static readonly string[] TextLikeContentTypes =
    {
        "text/",
        "application/json",
        "application/xml",
        "application/x-www-form-urlencoded",
    };

    public static int ByteLength(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return 0;
        }

        return Encoding.UTF8.GetByteCount(input);
    }

    public static string TruncateUtf8(string input, int maxBytes)
    {
        if (string.IsNullOrEmpty(input) || maxBytes <= 0)
        {
            return string.IsNullOrEmpty(input) ? input : string.Empty;
        }

        var bytes = Encoding.UTF8.GetBytes(input);
        if (bytes.Length <= maxBytes)
        {
            return input;
        }

        var end = maxBytes;
        while (end > 0 && (bytes[end] & 0xC0) == 0x80)
        {
            end--;
        }

        return Encoding.UTF8.GetString(bytes, 0, end);
    }

    public static Dictionary<string, string> NormalizeHeaders(IDictionary<string, string> headers)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (headers == null)
        {
            return result;
        }

        foreach (var pair in headers)
        {
            if (pair.Key == null)
            {
                continue;
            }

            result[pair.Key] = pair.Value ?? string.Empty;
        }

        return result;
    }

    public static Dictionary<string, string> LowercaseHeaders(IDictionary<string, string> headers)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (headers == null)
        {
            return result;
        }

        foreach (var pair in headers)
        {
            if (pair.Key == null)
            {
                continue;
            }

            result[pair.Key.ToLowerInvariant()] = pair.Value ?? string.Empty;
        }

        return result;
    }

    public static Dictionary<string, string> LimitHeaders(IDictionary<string, string> headers, int maxCount, int maxValueLength)
    {
        var limited = new Dictionary<string, string>(StringComparer.Ordinal);
        if (headers == null)
        {
            return limited;
        }

        var count = 0;
        foreach (var pair in headers)
        {
            if (count >= maxCount)
            {
                break;
            }

            var value = pair.Value ?? string.Empty;
            if (value.Length > maxValueLength)
            {
                value = value.Substring(0, maxValueLength);
            }

            limited[pair.Key] = value;
            count++;
        }

        return limited;
    }

    public static int? GetContentLength(IDictionary<string, string> headers)
    {
        if (headers == null)
        {
            return null;
        }

        string raw = null;
        foreach (var pair in headers)
        {
            if (string.Equals(pair.Key, "content-length", StringComparison.OrdinalIgnoreCase))
            {
                raw = pair.Value;
                break;
            }
        }

        if (string.IsNullOrEmpty(raw))
        {
            return null;
        }

        if (!int.TryParse(raw, out var parsed))
        {
            return null;
        }

        return parsed;
    }

    public static bool IsTextLikeContentType(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return false;
        }

        var lower = contentType.ToLowerInvariant();
        foreach (var type in TextLikeContentTypes)
        {
            if (lower.IndexOf(type, StringComparison.Ordinal) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    public static string GetHeader(IDictionary<string, string> headers, string name)
    {
        if (headers == null || string.IsNullOrEmpty(name))
        {
            return null;
        }

        foreach (var pair in headers)
        {
            if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Value;
            }
        }

        return null;
    }

    public static List<string> SanitizeUrlPatterns(IEnumerable<string> patterns)
    {
        if (patterns == null)
        {
            return null;
        }

        var sanitized = new List<string>();
        foreach (var pattern in patterns)
        {
            if (pattern == null)
            {
                continue;
            }

            var trimmed = pattern.Trim();
            if (trimmed.Length > 0)
            {
                sanitized.Add(trimmed);
            }
        }

        return sanitized.Count > 0 ? sanitized : null;
    }

    public static bool MatchesUrlPattern(string url, string pattern)
    {
        if (pattern.IndexOf('*') < 0)
        {
            return url.IndexOf(pattern, StringComparison.Ordinal) >= 0;
        }

        var pieces = pattern.Split('*');
        var position = 0;

        if (!pattern.StartsWith("*", StringComparison.Ordinal) && pieces[0].Length > 0)
        {
            if (!url.StartsWith(pieces[0], StringComparison.Ordinal))
            {
                return false;
            }

            position = pieces[0].Length;
        }

        for (var i = 1; i < pieces.Length; i++)
        {
            var piece = pieces[i];
            if (piece.Length == 0)
            {
                continue;
            }

            var foundAt = url.IndexOf(piece, position, StringComparison.Ordinal);
            if (foundAt < 0)
            {
                return false;
            }

            position = foundAt + piece.Length;
        }

        if (!pattern.EndsWith("*", StringComparison.Ordinal) && pieces.Length > 0)
        {
            var tail = pieces[pieces.Length - 1];
            if (tail.Length > 0 && !url.EndsWith(tail, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    public static bool MatchesURLFilters(string url, IList<string> allowlist, IList<string> denylist)
    {
        if (allowlist != null && allowlist.Count > 0)
        {
            var isAllowed = false;
            foreach (var pattern in allowlist)
            {
                if (MatchesUrlPattern(url, pattern))
                {
                    isAllowed = true;
                    break;
                }
            }

            if (!isAllowed)
            {
                return false;
            }
        }

        if (denylist != null && denylist.Count > 0)
        {
            foreach (var pattern in denylist)
            {
                if (MatchesUrlPattern(url, pattern))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static string EscapeJson(string value)
    {
        if (value == null)
        {
            return "null";
        }

        var sb = new StringBuilder(value.Length + 16);
        sb.Append('"');
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\b':
                    sb.Append("\\b");
                    break;
                case '\f':
                    sb.Append("\\f");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    if (c < 0x20)
                    {
                        sb.Append("\\u");
                        sb.Append(((int)c).ToString("x4"));
                    }
                    else
                    {
                        sb.Append(c);
                    }

                    break;
            }
        }

        sb.Append('"');
        return sb.ToString();
    }
}
