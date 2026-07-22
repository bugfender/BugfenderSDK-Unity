#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;

/// <summary>
/// iOS native entry points (BugfenderBridge.mm). Kept in a separate type to avoid duplicate P/Invoke declarations on <see cref="Bugfender"/>.
/// </summary>
internal static class BugfenderNativeIos
{
    [DllImport("__Internal", EntryPoint = "BugfenderSetSDKType")]
    internal static extern void SetSDKType(string sdkType, int version);

    [DllImport("__Internal", EntryPoint = "BugfenderActivateLogger")]
    internal static extern void ActivateLogger(string key, bool printToConsole, bool hideDeviceName, string apiURL, string baseURL);

    [DllImport("__Internal", EntryPoint = "BugfenderEnableUIEventLogging")]
    internal static extern void EnableUIEventLogging();

    [DllImport("__Internal", EntryPoint = "BugfenderEnableCrashReporting")]
    internal static extern void EnableCrashReporting();

    [DllImport("__Internal", EntryPoint = "BugfenderEnableNSLogLogging")]
    internal static extern void EnableNSLogLogging();

    [DllImport("__Internal", EntryPoint = "BugfenderSetDeviceString")]
    internal static extern void SetDeviceString(string key, string value);

    [DllImport("__Internal", EntryPoint = "BugfenderRemoveDeviceKey")]
    internal static extern void RemoveDeviceKey(string key);

    [DllImport("__Internal", EntryPoint = "BugfenderLog")]
    internal static extern void Log(int logLevel, string tag, string message);

    [DllImport("__Internal", EntryPoint = "BugfenderSendCrash")]
    internal static extern string SendCrash(string title, string text);

    [DllImport("__Internal", EntryPoint = "BugfenderSendIssue")]
    internal static extern string SendIssue(string title, string markdown);

    [DllImport("__Internal", EntryPoint = "BugfenderSendUserFeedback")]
    internal static extern string SendUserFeedback(string subject, string message);

    [DllImport("__Internal", EntryPoint = "BugfenderSetMaximumLocalStorageSize")]
    internal static extern void SetMaximumLocalStorageSize(ulong maximumLocalStorageSizeBytes);

    [DllImport("__Internal", EntryPoint = "BugfenderGetDeviceIdentifierUrl")]
    internal static extern string GetDeviceIdentifierUrl();

    [DllImport("__Internal", EntryPoint = "BugfenderGetSessionIdentifierUrl")]
    internal static extern string GetSessionIdentifierUrl();

    [DllImport("__Internal", EntryPoint = "BugfenderSetForceEnabled")]
    internal static extern void SetForceEnabled(bool enabled);

    [DllImport("__Internal", EntryPoint = "BugfenderForceSendOnce")]
    internal static extern void ForceSendOnce();

    [DllImport("__Internal", EntryPoint = "BugfenderGetSessionIdentifier")]
    internal static extern string GetSessionIdentifier();

    [DllImport("__Internal", EntryPoint = "BugfenderSetNetworkLoggingEnabled")]
    internal static extern void SetNetworkLoggingEnabled(bool enabled);

    [DllImport("__Internal", EntryPoint = "BugfenderSetNetworkLoggingCaptureBodies")]
    internal static extern void SetNetworkLoggingCaptureBodies(bool capture);

    [DllImport("__Internal", EntryPoint = "BugfenderSetNetworkLoggingCaptureErrorResponseBodies")]
    internal static extern void SetNetworkLoggingCaptureErrorResponseBodies(bool capture);

    [DllImport("__Internal", EntryPoint = "BugfenderSetNetworkLoggingURLFilter")]
    internal static extern void SetNetworkLoggingURLFilter(string allowlistJoined, string denylistJoined);

    [DllImport("__Internal", EntryPoint = "BugfenderSetNetworkLoggingMaxRequestsPerMinute")]
    internal static extern void SetNetworkLoggingMaxRequestsPerMinute(int countOrNegative);
}
#endif
