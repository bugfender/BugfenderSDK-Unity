using System;
using System.Collections.Generic;

/// <summary>
/// Request obfuscation: receive URL, headers and body; return possibly redacted values.
/// </summary>
public delegate NetworkRequestData NetworkLoggingRequestObfuscationHandler(
    string url,
    IDictionary<string, string> headers,
    string body);

/// <summary>
/// Response obfuscation: receive headers and body; return possibly redacted values.
/// </summary>
public delegate NetworkResponseData NetworkLoggingResponseObfuscationHandler(
    IDictionary<string, string> headers,
    string body);

/// <summary>Obfuscated request fields returned by a request obfuscation handler.</summary>
public struct NetworkRequestData
{
    public string Url;
    public IDictionary<string, string> Headers;
    public string Body;

    public NetworkRequestData(string url, IDictionary<string, string> headers, string body)
    {
        Url = url;
        Headers = headers;
        Body = body;
    }
}

/// <summary>Obfuscated response fields returned by a response obfuscation handler.</summary>
public struct NetworkResponseData
{
    public IDictionary<string, string> Headers;
    public string Body;

    public NetworkResponseData(IDictionary<string, string> headers, string body)
    {
        Headers = headers;
        Body = body;
    }
}

/// <summary>Optional timing breakdown for a captured network request. Omit unavailable phases.</summary>
public struct NetworkTimingBreakdown
{
    public long? DnsMs;
    public long? ConnectMs;
    public long? TlsMs;
    public long? TtfbMs;
    public long? DownloadMs;
}

/// <summary>Input for building a <c>bf_network</c> log entry.</summary>
public sealed class NetworkLogEntry
{
    public string Url;
    public string Method;
    public string RequestId;
    public long StartTimeMs;
    public long DurationMs;
    public int? StatusCode;
    public int? RequestSize;
    public int? ResponseSize;
    public IDictionary<string, string> RequestHeaders;
    public IDictionary<string, string> ResponseHeaders;
    public string RequestBody;
    public string ResponseBody;
    public bool IncludeRequestBody;
    public bool IncludeResponseBody;
    public string Error;
    public NetworkTimingBreakdown? Timing;
}
