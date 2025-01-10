using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.Diagnostics;

public class Bugfender : MonoBehaviour {

    public string APP_KEY;
    public bool ENABLE_UI_EVENT_LOGGING = false;
    public bool ENABLE_CRASH_REPORTING = true;
    public bool HIDE_DEVICE_NAME = false;
    public bool PRINT_TO_CONSOLE = false;
    public string API_URL;
    public string BASE_URL;

    public enum LogLevel { Debug, Warning, Error, Trace, Info, Fatal };

#if UNITY_ANDROID && !UNITY_EDITOR
	private static AndroidJavaClass bugfender;
#elif UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern void BugfenderActivateLogger(string key, bool printToConsole, bool hideDeviceName, string apiURL, string baseURL);
      
    [DllImport ("__Internal")]
    private static extern void BugfenderEnableUIEventLogging();

    [DllImport ("__Internal")]
    private static extern void BugfenderEnableCrashReporting();
  
    [DllImport ("__Internal")]
    private static extern void BugfenderSetDeviceString(string key, string value);
  
    [DllImport ("__Internal")]
    private static extern void BugfenderRemoveDeviceKey(string key);
  
    [DllImport ("__Internal")]
    private static extern void BugfenderLog(int logLevel, string tag, string message);
  
    [DllImport ("__Internal")]
    private static extern string BugfenderSendCrash(string title, string text);

    [DllImport ("__Internal")]
    private static extern string BugfenderSendIssue(string title, string markdown);

    [DllImport ("__Internal")]
    private static extern string BugfenderSendUserFeedback(string subject, string message);

    [DllImport ("__Internal")]
    private static extern void BugfenderSetMaximumLocalStorageSize(ulong maximumLocalStorageSizeBytes);

    [DllImport ("__Internal")]
    private static extern string BugfenderGetDeviceIdentifierUrl();

    [DllImport ("__Internal")]
    private static extern string BugfenderGetSessionIdentifierUrl();

    [DllImport ("__Internal")]
    private static extern void BugfenderSetForceEnabled(bool enabled);

    [DllImport ("__Internal")]
    private static extern void BugfenderForceSendOnce();
#endif

    // Automatically called when scene starts
    void Start()
    {
        Debug.Log("[BF] *** INITIALIZING BUGFENDER ***");
#if UNITY_ANDROID && !UNITY_EDITOR
		if (bugfender == null) {
			using (AndroidJavaClass activityClass = new AndroidJavaClass ("com.unity3d.player.UnityPlayer")) {
				var currentActivity = activityClass.GetStatic<AndroidJavaObject> ("currentActivity");

				bugfender = new AndroidJavaClass ("com.bugfender.sdk.Bugfender");
				if (bugfender != null) {
                                        if(HIDE_DEVICE_NAME) {
                                                bugfender.CallStatic ("overrideDeviceName", "Unknown");
                                        }
                                        if(API_URL.Length > 0 ) {
                                                bugfender.CallStatic ("setApiUrl", API_URL);
                                        }
                                        if(BASE_URL.Length > 0) {
                                                bugfender.CallStatic ("setBaseUrl", BASE_URL );
                                        }
					bugfender.CallStatic ("init", currentActivity, APP_KEY, PRINT_TO_CONSOLE );
                                        if(ENABLE_UI_EVENT_LOGGING) {
                                                var application = currentActivity.Call<AndroidJavaObject>("getApplication");
                                                bugfender.CallStatic ("enableUIEventLogging", application);
                                        }
                                        if (ENABLE_CRASH_REPORTING) {
                                                bugfender.CallStatic ("enableCrashReporting");
                                        }
                                        //bugfender.CallStatic ("enableLogcatLogging"); // optional, uncomment if you want it (Android only)
				}
                                        
			}
		}
#elif UNITY_IOS && !UNITY_EDITOR
        BugfenderActivateLogger(APP_KEY, PRINT_TO_CONSOLE, HIDE_DEVICE_NAME, API_URL, BASE_URL);
        if(ENABLE_UI_EVENT_LOGGING) {
                BugfenderEnableUIEventLogging();
        }
        if (ENABLE_CRASH_REPORTING) {
                BugfenderEnableCrashReporting();
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
        BugfenderSetDeviceString(key, value);
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
        BugfenderRemoveDeviceKey(key);
#else
        Debug.Log("[BF] Remove device key: " + key);
#endif
    }

    public static void Log(string message)
    {
        Log(LogLevel.Debug, "", message);
    }

    public static void Log(LogLevel logLevel, string tag, string message)
    {
        Debug.Log("[BF] TESTING: Sending log to Bugfender: [" + logLevel + "][" + tag + "] " + message);
#if UNITY_ANDROID && !UNITY_EDITOR
        if (bugfender != null) {
        AndroidJavaClass levelClass = new AndroidJavaClass ("com.bugfender.sdk.LogLevel");
            AndroidJavaObject level = levelClass.GetStatic<AndroidJavaObject>(logLevel.ToString());
            bugfender.CallStatic ("log", 0, "", "", level, tag, message);
        }
#elif UNITY_IOS && !UNITY_EDITOR
        int intLevel = (int)logLevel;
        BugfenderLog(intLevel, tag, message);
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
        return BugfenderSendCrash(title, text);
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
        return BugfenderSendIssue(title, text);
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
        return BugfenderSendUserFeedback(subject, message);
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
        BugfenderSetMaximumLocalStorageSize(bytes);
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
        return BugfenderGetDeviceIdentifierUrl();
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
        return BugfenderGetSessionIdentifierUrl();
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
        BugfenderSetForceEnabled(enabled);
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
        BugfenderForceSendOnce();
#else
        Debug.Log("[BF] Force send once");
#endif
    }

}