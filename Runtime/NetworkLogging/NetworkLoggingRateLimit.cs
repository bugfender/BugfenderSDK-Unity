using System;

/// <summary>
/// Rate limits network log capture per calendar minute (aligned with Android / JS SDKs).
/// </summary>
internal sealed class NetworkLoggingRateLimit
{
    private readonly Func<long> _getNowMs;
    private int _count;
    private long? _lastMinute;

    public NetworkLoggingRateLimit(Func<long> getNowMs = null)
    {
        _getNowMs = getNowMs ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    public bool ShouldCapture(int? maxRequestsPerMinute)
    {
        if (!maxRequestsPerMinute.HasValue)
        {
            return true;
        }

        var nowMinute = _getNowMs() / 60_000L;
        if (!_lastMinute.HasValue || _lastMinute.Value != nowMinute)
        {
            _lastMinute = nowMinute;
            _count = 0;
        }

        if (_count >= maxRequestsPerMinute.Value)
        {
            return false;
        }

        _count++;
        return true;
    }
}
