using System;
using System.Collections.Generic;

/// <summary>
/// Shared network logging config, filtering, rate limiting and emission for Unity HTTP traffic.
/// </summary>
internal static class NetworkLoggingManager
{
    public const string SessionHeaderName = "X-Bugfender-Session-ID";
    public const string RequestHeaderName = "X-Bugfender-Request-ID";

    private static readonly object Sync = new object();
    private static readonly NetworkLoggingRateLimit RateLimit = new NetworkLoggingRateLimit();

    private static bool _enabled;
    private static bool _captureBodies;
    private static bool _captureErrorResponseBodies;
    private static NetworkLoggingRequestObfuscationHandler _requestObfuscationHandler;
    private static NetworkLoggingResponseObfuscationHandler _responseObfuscationHandler;
    private static List<string> _allowlist;
    private static List<string> _denylist;
    private static int? _maxRequestsPerMinute;
    private static string _apiUrl = "https://api.bugfender.com";
    private static Func<string> _sessionIdProvider = () => null;

    public static bool Enabled
    {
        get { lock (Sync) { return _enabled; } }
    }

    public static bool CaptureBodies
    {
        get { lock (Sync) { return _captureBodies; } }
    }

    public static bool CaptureErrorResponseBodies
    {
        get { lock (Sync) { return _captureErrorResponseBodies; } }
    }

    public static void Configure(
        Func<string> sessionIdProvider = null,
        string apiUrl = null)
    {
        lock (Sync)
        {
            if (sessionIdProvider != null)
            {
                _sessionIdProvider = sessionIdProvider;
            }

            if (!string.IsNullOrWhiteSpace(apiUrl))
            {
                _apiUrl = apiUrl.Trim().TrimEnd('/');
            }
        }
    }

    public static void SetEnabled(bool enabled)
    {
        lock (Sync)
        {
            _enabled = enabled;
        }
    }

    public static void SetCaptureBodies(bool capture)
    {
        lock (Sync)
        {
            _captureBodies = capture;
        }
    }

    public static void SetCaptureErrorResponseBodies(bool capture)
    {
        lock (Sync)
        {
            _captureErrorResponseBodies = capture;
        }
    }

    public static void SetRequestObfuscationHandler(NetworkLoggingRequestObfuscationHandler handler)
    {
        lock (Sync)
        {
            _requestObfuscationHandler = handler;
        }
    }

    public static void SetResponseObfuscationHandler(NetworkLoggingResponseObfuscationHandler handler)
    {
        lock (Sync)
        {
            _responseObfuscationHandler = handler;
        }
    }

    public static void SetURLFilter(IEnumerable<string> allowlist, IEnumerable<string> denylist)
    {
        lock (Sync)
        {
            _allowlist = NetworkLoggingUtils.SanitizeUrlPatterns(allowlist);
            _denylist = NetworkLoggingUtils.SanitizeUrlPatterns(denylist);
        }
    }

    public static void SetMaxRequestsPerMinute(int? count)
    {
        lock (Sync)
        {
            if (count.HasValue && count.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Max requests per minute must be null or a positive integer.");
            }

            _maxRequestsPerMinute = count;
        }
    }

    public static string CreateRequestId()
    {
        return Guid.NewGuid().ToString();
    }

    public static string GetSessionId()
    {
        try
        {
            return _sessionIdProvider != null ? _sessionIdProvider() : null;
        }
        catch
        {
            return null;
        }
    }

    public static bool ShouldInjectHeaders()
    {
        return Enabled;
    }

    public static bool ShouldCapture(string url)
    {
        lock (Sync)
        {
            if (!_enabled)
            {
                return false;
            }

            if (IsBugfenderApi(url))
            {
                return false;
            }

            if (!NetworkLoggingUtils.MatchesURLFilters(url, _allowlist, _denylist))
            {
                return false;
            }

            return RateLimit.ShouldCapture(_maxRequestsPerMinute);
        }
    }

    public static bool ShouldCaptureResponseBody(string contentType, int? statusCode)
    {
        lock (Sync)
        {
            if (!NetworkLoggingUtils.IsTextLikeContentType(contentType))
            {
                return false;
            }

            if (_captureBodies)
            {
                return true;
            }

            return _captureErrorResponseBodies && statusCode.HasValue && statusCode.Value >= 400;
        }
    }

    public static bool ShouldCaptureRequestBody(string contentType)
    {
        lock (Sync)
        {
            return _captureBodies && NetworkLoggingUtils.IsTextLikeContentType(contentType);
        }
    }

    public static void Emit(NetworkLogEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        NetworkLoggingRequestObfuscationHandler requestHandler;
        NetworkLoggingResponseObfuscationHandler responseHandler;

        lock (Sync)
        {
            requestHandler = _requestObfuscationHandler;
            responseHandler = _responseObfuscationHandler;
        }

        var url = entry.Url ?? string.Empty;
        var requestHeaders = NetworkLoggingUtils.LowercaseHeaders(entry.RequestHeaders);
        var responseHeaders = NetworkLoggingUtils.LowercaseHeaders(entry.ResponseHeaders);
        var requestBody = entry.RequestBody;
        var responseBody = entry.ResponseBody;
        var includeRequestBody = entry.IncludeRequestBody;
        var includeResponseBody = entry.IncludeResponseBody;

        if (requestHandler != null)
        {
            var obfuscated = ApplyRequestObfuscation(requestHandler, url, requestHeaders, requestBody);
            url = obfuscated.Url;
            requestHeaders = NetworkLoggingUtils.LowercaseHeaders(obfuscated.Headers);
            requestBody = obfuscated.Body;
        }

        if (responseHandler != null)
        {
            var obfuscated = ApplyResponseObfuscation(responseHandler, responseHeaders, responseBody);
            responseHeaders = NetworkLoggingUtils.LowercaseHeaders(obfuscated.Headers);
            responseBody = obfuscated.Body;
        }

        var text = NetworkLoggingPayload.BuildLogText(new NetworkLogEntry
        {
            Url = url,
            Method = entry.Method,
            RequestId = entry.RequestId,
            StartTimeMs = entry.StartTimeMs,
            DurationMs = entry.DurationMs,
            StatusCode = entry.StatusCode,
            RequestSize = entry.RequestSize,
            ResponseSize = entry.ResponseSize,
            RequestHeaders = requestHeaders,
            ResponseHeaders = responseHeaders,
            RequestBody = requestBody,
            ResponseBody = responseBody,
            IncludeRequestBody = includeRequestBody,
            IncludeResponseBody = includeResponseBody,
            Error = entry.Error,
            Timing = entry.Timing,
        });

        Bugfender.Log(Bugfender.LogLevel.Info, NetworkLoggingPayload.NetworkLogTag, text);
    }

    private static NetworkRequestData ApplyRequestObfuscation(
        NetworkLoggingRequestObfuscationHandler handler,
        string url,
        IDictionary<string, string> headers,
        string body)
    {
        if (handler == null)
        {
            return new NetworkRequestData(url, headers, body);
        }

        try
        {
            var copy = new Dictionary<string, string>(headers);
            var result = handler(url, copy, body);
            return new NetworkRequestData(
                result.Url ?? url,
                result.Headers ?? new Dictionary<string, string>(),
                result.Body);
        }
        catch
        {
            return new NetworkRequestData(url, new Dictionary<string, string>(), null);
        }
    }

    private static NetworkResponseData ApplyResponseObfuscation(
        NetworkLoggingResponseObfuscationHandler handler,
        IDictionary<string, string> headers,
        string body)
    {
        if (handler == null)
        {
            return new NetworkResponseData(headers, body);
        }

        try
        {
            var copy = new Dictionary<string, string>(headers);
            var result = handler(copy, body);
            return new NetworkResponseData(
                result.Headers ?? new Dictionary<string, string>(),
                result.Body);
        }
        catch
        {
            return new NetworkResponseData(new Dictionary<string, string>(), null);
        }
    }

    private static bool IsBugfenderApi(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return false;
        }

        string apiUrl;
        lock (Sync)
        {
            apiUrl = _apiUrl;
        }

        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed))
            {
                return false;
            }

            if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var api))
            {
                return url.IndexOf("bugfender.com", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            if (!string.Equals(parsed.Scheme, api.Scheme, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(parsed.Authority, api.Authority, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var apiPath = api.AbsolutePath.TrimEnd('/');
            if (string.IsNullOrEmpty(apiPath) || apiPath == "/")
            {
                return true;
            }

            var path = parsed.AbsolutePath;
            return path == apiPath
                   || path.StartsWith(apiPath + "/", StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }
}
