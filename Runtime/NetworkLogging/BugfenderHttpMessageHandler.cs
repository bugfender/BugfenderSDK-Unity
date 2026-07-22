using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// <see cref="HttpMessageHandler"/> that injects Bugfender correlation headers and captures
/// network logs for <see cref="HttpClient"/> traffic when network logging is enabled.
/// </summary>
public sealed class BugfenderHttpMessageHandler : DelegatingHandler
{
    public BugfenderHttpMessageHandler()
        : this(new HttpClientHandler())
    {
    }

    public BugfenderHttpMessageHandler(HttpMessageHandler innerHandler)
        : base(innerHandler ?? new HttpClientHandler())
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var url = request.RequestUri != null ? request.RequestUri.ToString() : string.Empty;
        var method = request.Method != null ? request.Method.Method : "GET";
        var startTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var stopwatch = Stopwatch.StartNew();

        string requestId = null;
        var shouldInject = NetworkLoggingManager.ShouldInjectHeaders();
        if (shouldInject)
        {
            requestId = NetworkLoggingManager.CreateRequestId();
            var sessionId = NetworkLoggingManager.GetSessionId();
            if (!string.IsNullOrEmpty(sessionId))
            {
                request.Headers.Remove(NetworkLoggingManager.SessionHeaderName);
                request.Headers.TryAddWithoutValidation(NetworkLoggingManager.SessionHeaderName, sessionId);
            }

            request.Headers.Remove(NetworkLoggingManager.RequestHeaderName);
            request.Headers.TryAddWithoutValidation(NetworkLoggingManager.RequestHeaderName, requestId);
        }

        var shouldCapture = NetworkLoggingManager.ShouldCapture(url);
        Dictionary<string, string> requestHeaders = null;
        string requestBody = null;
        int? requestSize = null;
        var includeRequestBody = false;

        if (shouldCapture)
        {
            requestHeaders = ExtractRequestHeaders(request);
            requestSize = NetworkLoggingUtils.GetContentLength(requestHeaders);
            includeRequestBody = NetworkLoggingManager.ShouldCaptureRequestBody(
                NetworkLoggingUtils.GetHeader(requestHeaders, "content-type"));

            if (includeRequestBody && request.Content != null)
            {
                try
                {
                    await request.Content.LoadIntoBufferAsync().ConfigureAwait(false);
                    requestBody = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!requestSize.HasValue && requestBody != null)
                    {
                        requestSize = NetworkLoggingUtils.ByteLength(requestBody);
                    }
                }
                catch
                {
                    requestBody = null;
                }
            }
        }

        try
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            if (shouldCapture)
            {
                var responseHeaders = ExtractResponseHeaders(response);
                var statusCode = (int)response.StatusCode;
                var includeResponseBody = NetworkLoggingManager.ShouldCaptureResponseBody(
                    NetworkLoggingUtils.GetHeader(responseHeaders, "content-type"),
                    statusCode);
                string responseBody = null;
                var responseSize = NetworkLoggingUtils.GetContentLength(responseHeaders);

                if (includeResponseBody && response.Content != null)
                {
                    try
                    {
                        await response.Content.LoadIntoBufferAsync().ConfigureAwait(false);
                        responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (!responseSize.HasValue && responseBody != null)
                        {
                            responseSize = NetworkLoggingUtils.ByteLength(responseBody);
                        }
                    }
                    catch
                    {
                        responseBody = null;
                    }
                }

                NetworkLoggingManager.Emit(new NetworkLogEntry
                {
                    Url = url,
                    Method = method,
                    RequestId = requestId ?? NetworkLoggingManager.CreateRequestId(),
                    StartTimeMs = startTimeMs,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    StatusCode = statusCode,
                    RequestSize = requestSize,
                    ResponseSize = responseSize,
                    RequestHeaders = requestHeaders,
                    ResponseHeaders = responseHeaders,
                    RequestBody = requestBody,
                    ResponseBody = responseBody,
                    IncludeRequestBody = includeRequestBody,
                    IncludeResponseBody = includeResponseBody,
                });
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            if (shouldCapture)
            {
                NetworkLoggingManager.Emit(new NetworkLogEntry
                {
                    Url = url,
                    Method = method,
                    RequestId = requestId ?? NetworkLoggingManager.CreateRequestId(),
                    StartTimeMs = startTimeMs,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    RequestSize = requestSize,
                    RequestHeaders = requestHeaders ?? new Dictionary<string, string>(),
                    ResponseHeaders = new Dictionary<string, string>(),
                    RequestBody = requestBody,
                    IncludeRequestBody = includeRequestBody,
                    IncludeResponseBody = false,
                    Error = ex.Message,
                });
            }

            throw;
        }
    }

    private static Dictionary<string, string> ExtractRequestHeaders(HttpRequestMessage request)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in request.Headers)
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }

        if (request.Content != null)
        {
            foreach (var header in request.Content.Headers)
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }
        }

        return headers;
    }

    private static Dictionary<string, string> ExtractResponseHeaders(HttpResponseMessage response)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in response.Headers)
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }

        if (response.Content != null)
        {
            foreach (var header in response.Content.Headers)
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }
        }

        return headers;
    }
}
