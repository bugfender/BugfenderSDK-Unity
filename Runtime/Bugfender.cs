using UnityEngine;
using UnityEngine.Diagnostics;

public class Bugfender : MonoBehaviour {
    private const string SDK_TYPE = "unity";
    private const int SDK_TYPE_VERSION = 30001;

    public string APP_KEY;
    public bool ENABLE_UI_EVENT_LOGGING = false;
    public bool ENABLE_CRASH_REPORTING = true;
    public bool HIDE_DEVICE_NAME = false;
    public bool AUTO_ANDROID_ID = false;
    public bool PRINT_TO_CONSOLE = false;
    public string API_URL;
    public string BASE_URL;

    public enum LogLevel { Debug, Warning, Error, Trace, Info, Fatal };

    private static bool BugfenderResourceFlagTrue(string resourceName)
    {
        var asset = Resources.Load<TextAsset>(resourceName);
        return asset != null && string.Equals(asset.text.Trim(), "true", System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool ResourcesWantNativeLogCapture()
    {
        return BugfenderResourceFlagTrue("bugfender_native_log_capture");
    }

#if UNITY_ANDROID && !UNITY_EDITOR
	private static AndroidJavaClass bugfender;
#endif

    // Automatically called when scene starts
    void Start()
    {
        // Optional override from Resources/bugfender_app_key.txt (e.g. for CI or per-build config)
        var keyAsset = Resources.Load<TextAsset>("bugfender_app_key");
        if (keyAsset != null && !string.IsNullOrWhiteSpace(keyAsset.text))
        {
            APP_KEY = keyAsset.text.Trim();
        }
        // Optional: set Resources/bugfender_print_to_console.txt to "true" to mirror logs to logcat (Android) / Xcode console (iOS)
        var printAsset = Resources.Load<TextAsset>("bugfender_print_to_console");
        if (printAsset != null && string.Equals(printAsset.text.Trim(), "true", System.StringComparison.OrdinalIgnoreCase))
        {
            PRINT_TO_CONSOLE = true;
        }
        Debug.Log("[BF] *** INITIALIZING BUGFENDER ***");
#if UNITY_ANDROID && !UNITY_EDITOR
		if (bugfender == null) {
			using (AndroidJavaClass activityClass = new AndroidJavaClass ("com.unity3d.player.UnityPlayer")) {
				var currentActivity = activityClass.GetStatic<AndroidJavaObject> ("currentActivity");

				bugfender = new AndroidJavaClass ("com.bugfender.sdk.Bugfender");
				if (bugfender != null) {
                                        try {
                                                bugfender.CallStatic ("setSDKType", SDK_TYPE, SDK_TYPE_VERSION);
                                        } catch (AndroidJavaException) {
                                                Debug.LogWarning("[BF] Bugfender.setSDKType is not available in the Android SDK.");
                                        }
                                        if(HIDE_DEVICE_NAME) {
                                                bugfender.CallStatic ("overrideDeviceName", "Unknown");
                                        }
                                        if(API_URL.Length > 0 ) {
                                                bugfender.CallStatic ("setApiUrl", API_URL);
                                        }
                                        if(BASE_URL.Length > 0) {
                                                bugfender.CallStatic ("setBaseUrl", BASE_URL );
                                        }
					bugfender.CallStatic ("init", currentActivity, APP_KEY, PRINT_TO_CONSOLE, AUTO_ANDROID_ID);
                                        if(ENABLE_UI_EVENT_LOGGING) {
                                                var application = currentActivity.Call<AndroidJavaObject>("getApplication");
                                                bugfender.CallStatic ("enableUIEventLogging", application);
                                        }
                                        if (ENABLE_CRASH_REPORTING) {
                                                bugfender.CallStatic ("enableCrashReporting");
                                        }
                                        // Optional: set Resources/bugfender_debug.txt to "true" to enable native SDK debug logs (tag BF/DEBUG in logcat)
                                        var debugAsset = Resources.Load<TextAsset>("bugfender_debug");
                                        if (debugAsset != null && string.Equals(debugAsset.text.Trim(), "true", System.StringComparison.OrdinalIgnoreCase)) {
                                                try { bugfender.CallStatic("setDebugMode", true); } catch (AndroidJavaException) { /* ignore if not available */ }
                                        }
                                        if (ResourcesWantNativeLogCapture()) {
                                                try { bugfender.CallStatic("enableLogcatLogging"); } catch (AndroidJavaException) { }
                                        }
				}
                                        
			}
		}
#elif UNITY_IOS && !UNITY_EDITOR
        BugfenderNativeIos.SetSDKType(SDK_TYPE, SDK_TYPE_VERSION);
        BugfenderNativeIos.ActivateLogger(APP_KEY, PRINT_TO_CONSOLE, HIDE_DEVICE_NAME, API_URL, BASE_URL);
        if(ENABLE_UI_EVENT_LOGGING) {
                BugfenderNativeIos.EnableUIEventLogging();
        }
        if (ENABLE_CRASH_REPORTING) {
                BugfenderNativeIos.EnableCrashReporting();
        }
        if (ResourcesWantNativeLogCapture()) {
                BugfenderNativeIos.EnableNSLogLogging();
        }
#endif
        /* Some examples on how to use Bugfender:
         *   Bugfender.Log("BF Initialized");
         *   Bugfender.SetDeviceString("key","value");
         *   Bugfender.SetDeviceString("key2", "will remove");
         *   Bugfender.RemoveDeviceKey("key2");
         *    Bugfender.SendIssue("test", "this is a test");
         *    Utils.ForceCrash(ForcedCrashCategory.Abort); // test crash
        */
    }

    public static void SetDeviceString(string key, string value)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (bugfender != null) {
            bugfender.CallStatic ("setDeviceString", key, value);
        }
#elif UNITY_IOS && !UNITY_EDITOR
        BugfenderNativeIos.SetDeviceString(key, value);
#else
        Debug.Log("[BF] Set device key:" + key + " value:" + value);
#endif
    }

    public static void RemoveDeviceKey(string key)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (bugfender != null) {
            bugfender.CallStatic ("removeDeviceKey", key);
        }
#elif UNITY_IOS && !UNITY_EDITOR
        BugfenderNativeIos.RemoveDeviceKey(key);
#else
        Debug.Log("[BF] Remove device key: " + key);
#endif
    }

    /// <summary>
    /// Enables native system log capture for the current platform (Android logcat; iOS 15+ NSLog/OSLog).
    /// Call after the Bugfender component has initialized, or add <c>Assets/Resources/bugfender_native_log_capture.txt</c> with <c>true</c>.
    /// </summary>
    public static void EnableNativeLogCapture()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (bugfender != null) {
            try { bugfender.CallStatic("enableLogcatLogging"); } catch (AndroidJavaException) { }
        }
#elif UNITY_IOS && !UNITY_EDITOR
        BugfenderNativeIos.EnableNSLogLogging();
#else
        Debug.Log("[BF] EnableNativeLogCapture is for Android or iOS device builds only.");
#endif
    }

    public static void Log(string message)
    {
        Log(LogLevel.Debug, "", message);
    }

    public static void Log(LogLevel logLevel, string tag, string message)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (bugfender != null) {
        AndroidJavaClass levelClass = new AndroidJavaClass ("com.bugfender.sdk.LogLevel");
            AndroidJavaObject level = levelClass.GetStatic<AndroidJavaObject>(logLevel.ToString());
            bugfender.CallStatic ("log", 0, "", "", level, tag, message);
        }
#elif UNITY_IOS && !UNITY_EDITOR
        int intLevel = (int)logLevel;
        BugfenderNativeIos.Log(intLevel, tag, message);
#else
        Debug.Log("[BF] Sending log to Bugfender: [" + logLevel + "][" + tag + "] " + message);
#endif
    }

    public static string SendCrash(string title, string text) {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (bugfender != null) {
            var url = bugfender.CallStatic<AndroidJavaObject> ("sendCrash", title, text);
            return url.Call<string> ("toString");
        }
        return null;
#elif UNITY_IOS && !UNITY_EDITOR
        return BugfenderNativeIos.SendCrash(title, text);
#else
        Debug.Log("[BF] Send crash: " + title + " : " + text);
        return null;
#endif
    }

    public static string SendIssue(string title, string text) {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (bugfender != null) {
            var url = bugfender.CallStatic<AndroidJavaObject> ("sendIssue", title, text);
            return url.Call<string> ("toString");
        }
        return null;
#elif UNITY_IOS && !UNITY_EDITOR
        return BugfenderNativeIos.SendIssue(title, text);
#else
        Debug.Log("[BF] Send issue: " + title + " : " + text);
        return null;
#endif
    }

    public static string SendUserFeedback(string subject, string message) {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (bugfender != null) {
            var url = bugfender.CallStatic<AndroidJavaObject> ("sendUserFeedback", subject, message);
            return url.Call<string> ("toString");
        }
        return null;
#elif UNITY_IOS && !UNITY_EDITOR
        return BugfenderNativeIos.SendUserFeedback(subject, message);
#else
        Debug.Log("[BF] Send user feedback: " + subject + " : " + message);
        return null;
#endif
    }

    public static void SetMaximumLocalStorageSize(ulong bytes)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (bugfender != null) {
            var b = (long)bytes; // the java function wants a long
            bugfender.CallStatic ("setMaximumLocalStorageSize", b);
        }
#elif UNITY_IOS && !UNITY_EDITOR
        BugfenderNativeIos.SetMaximumLocalStorageSize(bytes);
#else
        Debug.Log("[BF] Set max storage size:" + bytes);
#endif
    }

    public static string DeviceIdentifierUrl() {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (bugfender != null) {
            var url = bugfender.CallStatic<AndroidJavaObject> ("getDeviceUrl");
            return url.Call<string> ("toString");
        }
        return null;
#elif UNITY_IOS && !UNITY_EDITOR
        return BugfenderNativeIos.GetDeviceIdentifierUrl();
#else
        return null;
#endif
    }

    public static string SessionIdentifierUrl() {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (bugfender != null) {
            var url = bugfender.CallStatic<AndroidJavaObject> ("getSessionUrl");
            return url.Call<string> ("toString");
        }
        return null;
#elif UNITY_IOS && !UNITY_EDITOR
        return BugfenderNativeIos.GetSessionIdentifierUrl();
#else
        return null;
#endif
    }

    public static void SetForceEnabled(bool enabled)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (bugfender != null) {
            bugfender.CallStatic ("setForceEnabled", enabled);
        }
#elif UNITY_IOS && !UNITY_EDITOR
        BugfenderNativeIos.SetForceEnabled(enabled);
#else
        Debug.Log("[BF] Set force enabled:" + enabled);
#endif
    }

    public static void ForceSendOnce()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (bugfender != null) {
            bugfender.CallStatic ("forceSendOnce");
        }
#elif UNITY_IOS && !UNITY_EDITOR
        BugfenderNativeIos.ForceSendOnce();
#else
        Debug.Log("[BF] Force send once");
#endif
    }

    public static void SetSDKType(string sdkType, int version)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (bugfender != null) {
            bugfender.CallStatic ("setSDKType", sdkType, version);
        }
#elif UNITY_IOS && !UNITY_EDITOR
        BugfenderNativeIos.SetSDKType(sdkType, version);
#else
        Debug.Log("[BF] Set SDK type: " + sdkType + " version: " + version);
#endif
    }

}
