using System.Collections.Generic;
using System.Text;

internal static class NetworkLoggingPayload
{
    internal const string NetworkLogTag = "bf_network";
    private const int MaxLogTextSizeBytes = 32_000;
    private const int MaxBodySizeBytes = 4_096;
    private const int MaxHeaderValueSize = 256;
    private const int MaxHeaders = 40;
    private const string PayloadExceededErrorJson =
        "{\"error\":\"Network log omitted because payload exceeded max size\"}";

    public static string BuildLogText(NetworkLogEntry input)
    {
        var payload = FitPayload(ToPayload(input));
        var text = Serialize(payload);
        return NetworkLoggingUtils.ByteLength(text) <= MaxLogTextSizeBytes
            ? text
            : PayloadExceededErrorJson;
    }

    private sealed class Payload
    {
        public string Url;
        public string Method;
        public string RequestId;
        public long StartTime;
        public long DurationMs;
        public int? StatusCode;
        public int? RequestSize;
        public int? ResponseSize;
        public Dictionary<string, string> RequestHeaders = new Dictionary<string, string>();
        public Dictionary<string, string> ResponseHeaders = new Dictionary<string, string>();
        public string RequestBody;
        public bool HasRequestBody;
        public string ResponseBody;
        public bool HasResponseBody;
        public string Error;
        public NetworkTimingBreakdown Timing;
    }

    private static Payload ToPayload(NetworkLogEntry input)
    {
        var payload = new Payload
        {
            Url = input.Url ?? string.Empty,
            Method = string.IsNullOrEmpty(input.Method) ? "GET" : input.Method.ToUpperInvariant(),
            RequestId = input.RequestId ?? string.Empty,
            StartTime = input.StartTimeMs,
            DurationMs = input.DurationMs < 0 ? 0 : input.DurationMs,
            StatusCode = input.StatusCode,
            RequestSize = input.RequestSize,
            ResponseSize = input.ResponseSize,
            RequestHeaders = NetworkLoggingUtils.NormalizeHeaders(input.RequestHeaders),
            ResponseHeaders = NetworkLoggingUtils.NormalizeHeaders(input.ResponseHeaders),
            Error = string.IsNullOrEmpty(input.Error) ? null : input.Error,
            Timing = input.Timing ?? default,
        };

        if (input.IncludeRequestBody)
        {
            payload.HasRequestBody = true;
            payload.RequestBody = input.RequestBody == null
                ? null
                : NetworkLoggingUtils.TruncateUtf8(input.RequestBody, MaxBodySizeBytes);
        }

        if (input.IncludeResponseBody)
        {
            payload.HasResponseBody = true;
            payload.ResponseBody = input.ResponseBody == null
                ? null
                : NetworkLoggingUtils.TruncateUtf8(input.ResponseBody, MaxBodySizeBytes);
        }

        return payload;
    }

    private static bool SizeFits(Payload payload)
    {
        return NetworkLoggingUtils.ByteLength(Serialize(payload)) <= MaxLogTextSizeBytes;
    }

    private static Payload FitPayload(Payload payload)
    {
        if (SizeFits(payload))
        {
            return payload;
        }

        payload.HasResponseBody = false;
        payload.ResponseBody = null;
        if (SizeFits(payload))
        {
            return payload;
        }

        payload.HasRequestBody = false;
        payload.RequestBody = null;
        if (SizeFits(payload))
        {
            return payload;
        }

        payload.ResponseHeaders = NetworkLoggingUtils.LimitHeaders(payload.ResponseHeaders, MaxHeaders, MaxHeaderValueSize);
        payload.RequestHeaders = NetworkLoggingUtils.LimitHeaders(payload.RequestHeaders, MaxHeaders, MaxHeaderValueSize);
        if (SizeFits(payload))
        {
            return payload;
        }

        payload.ResponseHeaders = new Dictionary<string, string>();
        if (SizeFits(payload))
        {
            return payload;
        }

        payload.RequestHeaders = new Dictionary<string, string>();
        if (SizeFits(payload))
        {
            return payload;
        }

        payload.Url = NetworkLoggingUtils.TruncateUtf8(payload.Url, 2_048);
        if (payload.Error != null)
        {
            payload.Error = NetworkLoggingUtils.TruncateUtf8(payload.Error, 1_024);
        }

        if (SizeFits(payload))
        {
            return payload;
        }

        var minimal = new Payload
        {
            Url = NetworkLoggingUtils.TruncateUtf8(payload.Url, 512),
            Method = NetworkLoggingUtils.TruncateUtf8(payload.Method, 32),
            RequestId = payload.RequestId,
            StartTime = payload.StartTime,
            DurationMs = payload.DurationMs,
            StatusCode = payload.StatusCode,
        };

        if (SizeFits(minimal))
        {
            return minimal;
        }

        return null;
    }

    private static string Serialize(Payload payload)
    {
        if (payload == null)
        {
            return PayloadExceededErrorJson;
        }

        var sb = new StringBuilder(512);
        sb.Append('{');
        AppendString(sb, "url", payload.Url, false);
        AppendString(sb, "method", payload.Method, true);
        AppendString(sb, "request_id", payload.RequestId, true);
        AppendNumber(sb, "start_time", payload.StartTime, true);
        AppendNumber(sb, "duration_ms", payload.DurationMs, true);

        if (payload.StatusCode.HasValue)
        {
            AppendNumber(sb, "status_code", payload.StatusCode.Value, true);
        }

        if (payload.RequestSize.HasValue)
        {
            AppendNumber(sb, "request_size", payload.RequestSize.Value, true);
        }

        if (payload.ResponseSize.HasValue)
        {
            AppendNumber(sb, "response_size", payload.ResponseSize.Value, true);
        }

        AppendObject(sb, "request_headers", payload.RequestHeaders, true);
        AppendObject(sb, "response_headers", payload.ResponseHeaders, true);

        if (payload.HasRequestBody)
        {
            AppendNullableString(sb, "request_body", payload.RequestBody, true);
        }

        if (payload.HasResponseBody)
        {
            AppendNullableString(sb, "response_body", payload.ResponseBody, true);
        }

        if (!string.IsNullOrEmpty(payload.Error))
        {
            AppendString(sb, "error", payload.Error, true);
        }

        AppendTiming(sb, payload.Timing, true);
        sb.Append('}');
        return sb.ToString();
    }

    private static void AppendString(StringBuilder sb, string key, string value, bool leadingComma)
    {
        if (leadingComma)
        {
            sb.Append(',');
        }

        sb.Append(NetworkLoggingUtils.EscapeJson(key));
        sb.Append(':');
        sb.Append(NetworkLoggingUtils.EscapeJson(value ?? string.Empty));
    }

    private static void AppendNullableString(StringBuilder sb, string key, string value, bool leadingComma)
    {
        if (leadingComma)
        {
            sb.Append(',');
        }

        sb.Append(NetworkLoggingUtils.EscapeJson(key));
        sb.Append(':');
        sb.Append(value == null ? "null" : NetworkLoggingUtils.EscapeJson(value));
    }

    private static void AppendNumber(StringBuilder sb, string key, long value, bool leadingComma)
    {
        if (leadingComma)
        {
            sb.Append(',');
        }

        sb.Append(NetworkLoggingUtils.EscapeJson(key));
        sb.Append(':');
        sb.Append(value);
    }

    private static void AppendObject(StringBuilder sb, string key, IDictionary<string, string> headers, bool leadingComma)
    {
        if (leadingComma)
        {
            sb.Append(',');
        }

        sb.Append(NetworkLoggingUtils.EscapeJson(key));
        sb.Append(":{");
        if (headers != null)
        {
            var first = true;
            foreach (var pair in headers)
            {
                if (!first)
                {
                    sb.Append(',');
                }

                first = false;
                sb.Append(NetworkLoggingUtils.EscapeJson(pair.Key));
                sb.Append(':');
                sb.Append(NetworkLoggingUtils.EscapeJson(pair.Value ?? string.Empty));
            }
        }

        sb.Append('}');
    }

    private static void AppendTiming(StringBuilder sb, NetworkTimingBreakdown timing, bool leadingComma)
    {
        if (leadingComma)
        {
            sb.Append(',');
        }

        sb.Append("\"timing\":{");
        var first = true;
        AppendTimingField(sb, "dns_ms", timing.DnsMs, ref first);
        AppendTimingField(sb, "connect_ms", timing.ConnectMs, ref first);
        AppendTimingField(sb, "tls_ms", timing.TlsMs, ref first);
        AppendTimingField(sb, "ttfb_ms", timing.TtfbMs, ref first);
        AppendTimingField(sb, "download_ms", timing.DownloadMs, ref first);
        sb.Append('}');
    }

    private static void AppendTimingField(StringBuilder sb, string key, long? value, ref bool first)
    {
        if (!value.HasValue || value.Value < 0)
        {
            return;
        }

        if (!first)
        {
            sb.Append(',');
        }

        first = false;
        sb.Append(NetworkLoggingUtils.EscapeJson(key));
        sb.Append(':');
        sb.Append(value.Value);
    }
}
