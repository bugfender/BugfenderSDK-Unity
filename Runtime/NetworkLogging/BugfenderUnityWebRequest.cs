using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine.Networking;

/// <summary>
/// Helpers to instrument <see cref="UnityWebRequest"/> for Bugfender network logging.
/// Unity has no global HTTP interceptor; call these helpers (or <see cref="Bugfender.LogNetwork"/>)
/// around your requests.
/// </summary>
public static class BugfenderUnityWebRequest
{
    /// <summary>
    /// Injects correlation headers when network logging is enabled. Call before
    /// <see cref="UnityWebRequest.SendWebRequest"/>.
    /// </summary>
    public static string Prepare(UnityWebRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (!NetworkLoggingManager.ShouldInjectHeaders())
        {
            return null;
        }

        var requestId = NetworkLoggingManager.CreateRequestId();
        var sessionId = NetworkLoggingManager.GetSessionId();
        if (!string.IsNullOrEmpty(sessionId))
        {
            request.SetRequestHeader(NetworkLoggingManager.SessionHeaderName, sessionId);
        }

        request.SetRequestHeader(NetworkLoggingManager.RequestHeaderName, requestId);
        return requestId;
    }

    /// <summary>
    /// Captures a completed (or failed) <see cref="UnityWebRequest"/> as a network log when enabled.
    /// Pass <paramref name="shouldCapture"/> from a prior <see cref="ShouldCapture"/> check so rate
    /// limits are applied at request start (same as <see cref="BugfenderHttpMessageHandler"/>).
    /// </summary>
    public static void Complete(
        UnityWebRequest request,
        string requestId,
        long startTimeMs,
        long durationMs,
        string requestBody = null,
        IDictionary<string, string> requestHeaders = null,
        bool? shouldCapture = null)
    {
        if (request == null)
        {
            return;
        }

        var url = request.url ?? string.Empty;
        var capture = shouldCapture ?? NetworkLoggingManager.ShouldCapture(url);
        if (!capture)
        {
            return;
        }

        var headers = requestHeaders != null
            ? new Dictionary<string, string>(requestHeaders)
            : new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(requestId)
            && !headers.ContainsKey(NetworkLoggingManager.RequestHeaderName))
        {
            headers[NetworkLoggingManager.RequestHeaderName] = requestId;
        }

        var sessionId = NetworkLoggingManager.GetSessionId();
        if (!string.IsNullOrEmpty(sessionId)
            && !headers.ContainsKey(NetworkLoggingManager.SessionHeaderName))
        {
            headers[NetworkLoggingManager.SessionHeaderName] = sessionId;
        }

        var responseHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var all = request.GetResponseHeaders();
            if (all != null)
            {
                foreach (var pair in all)
                {
                    responseHeaders[pair.Key] = pair.Value;
                }
            }
        }
        catch
        {
            // ignored
        }

        int? statusCode = null;
        if (request.responseCode > 0)
        {
            statusCode = (int)request.responseCode;
        }

        var contentType = NetworkLoggingUtils.GetHeader(responseHeaders, "content-type");
        var includeRequestBody = NetworkLoggingManager.ShouldCaptureRequestBody(
            NetworkLoggingUtils.GetHeader(headers, "content-type"));
        var includeResponseBody = NetworkLoggingManager.ShouldCaptureResponseBody(contentType, statusCode);

        string responseBody = null;
        if (includeResponseBody)
        {
            try
            {
                responseBody = request.downloadHandler != null ? request.downloadHandler.text : null;
            }
            catch
            {
                responseBody = null;
            }
        }

        string error = null;
        if (request.result == UnityWebRequest.Result.ConnectionError
            || request.result == UnityWebRequest.Result.DataProcessingError)
        {
            error = request.error;
        }
        else if (!statusCode.HasValue && !string.IsNullOrEmpty(request.error))
        {
            error = request.error;
        }

        var requestSize = NetworkLoggingUtils.GetContentLength(headers);
        if (!requestSize.HasValue && includeRequestBody && requestBody != null)
        {
            requestSize = NetworkLoggingUtils.ByteLength(requestBody);
        }

        var responseSize = NetworkLoggingUtils.GetContentLength(responseHeaders);
        if (!responseSize.HasValue && includeResponseBody && responseBody != null)
        {
            responseSize = NetworkLoggingUtils.ByteLength(responseBody);
        }

        NetworkLoggingManager.Emit(new NetworkLogEntry
        {
            Url = url,
            Method = string.IsNullOrEmpty(request.method) ? "GET" : request.method.ToUpperInvariant(),
            RequestId = string.IsNullOrEmpty(requestId) ? NetworkLoggingManager.CreateRequestId() : requestId,
            StartTimeMs = startTimeMs,
            DurationMs = durationMs < 0 ? 0 : durationMs,
            StatusCode = statusCode,
            RequestSize = requestSize,
            ResponseSize = responseSize,
            RequestHeaders = headers,
            ResponseHeaders = responseHeaders,
            RequestBody = includeRequestBody ? requestBody : null,
            ResponseBody = includeResponseBody ? responseBody : null,
            IncludeRequestBody = includeRequestBody,
            IncludeResponseBody = includeResponseBody,
            Error = error,
        });
    }

    /// <summary>
    /// Returns whether the URL should be captured under current filters and rate limits.
    /// </summary>
    public static bool ShouldCapture(string url)
    {
        return NetworkLoggingManager.ShouldCapture(url ?? string.Empty);
    }

    /// <summary>
    /// Sends a <see cref="UnityWebRequest"/> while capturing a network log when enabled.
    /// Usage: <c>yield return BugfenderUnityWebRequest.Send(request);</c>
    /// </summary>
    public static IEnumerator Send(UnityWebRequest request, string requestBody = null, IDictionary<string, string> requestHeaders = null)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var startTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var stopwatch = Stopwatch.StartNew();
        var requestId = Prepare(request);
        var shouldCapture = ShouldCapture(request.url);

        var capturedHeaders = requestHeaders != null
            ? new Dictionary<string, string>(requestHeaders)
            : new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(requestId))
        {
            capturedHeaders[NetworkLoggingManager.RequestHeaderName] = requestId;
            var sessionId = NetworkLoggingManager.GetSessionId();
            if (!string.IsNullOrEmpty(sessionId))
            {
                capturedHeaders[NetworkLoggingManager.SessionHeaderName] = sessionId;
            }
        }

        var operation = request.SendWebRequest();
        while (!operation.isDone)
        {
            yield return null;
        }

        stopwatch.Stop();
        Complete(request, requestId, startTimeMs, stopwatch.ElapsedMilliseconds, requestBody, capturedHeaders, shouldCapture);
    }

    /// <summary>
    /// Convenience helper for JSON POST requests with network logging.
    /// </summary>
    public static UnityWebRequest PostJson(string url, string jsonBody)
    {
        var bodyRaw = Encoding.UTF8.GetBytes(jsonBody ?? string.Empty);
        var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
        {
            uploadHandler = new UploadHandlerRaw(bodyRaw),
            downloadHandler = new DownloadHandlerBuffer(),
        };
        request.SetRequestHeader("Content-Type", "application/json");
        return request;
    }
}
