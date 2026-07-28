using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

/// <summary>
/// Forwards Unity obfuscation handlers to the native Android / iOS Bugfender SDKs
/// so OkHttp / URLSession capture runs the same C# redaction callbacks.
/// </summary>
internal static class NetworkLoggingNativeBridge
{
    private static bool _iosCallbacksRegistered;

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaClass _bridgeClass;
    private static RequestObfuscatorProxy _requestProxy;
    private static ResponseObfuscatorProxy _responseProxy;
#endif

    public static void SyncObfuscationHandlers(
        NetworkLoggingRequestObfuscationHandler requestHandler,
        NetworkLoggingResponseObfuscationHandler responseHandler)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        SyncAndroid(requestHandler, responseHandler);
#elif UNITY_IOS && !UNITY_EDITOR
        SyncIos(requestHandler, responseHandler);
#else
        // Editor / unsupported platforms: C# capture path only.
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static void SyncAndroid(
        NetworkLoggingRequestObfuscationHandler requestHandler,
        NetworkLoggingResponseObfuscationHandler responseHandler)
    {
        try
        {
            if (_bridgeClass == null)
            {
                _bridgeClass = new AndroidJavaClass("com.bugfender.unity.androidlib.UnityNetworkObfuscationBridge");
            }

            if (requestHandler != null)
            {
                _requestProxy = new RequestObfuscatorProxy(requestHandler);
                _bridgeClass.CallStatic("setRequestObfuscationHandler", _requestProxy);
            }
            else
            {
                _requestProxy = null;
                _bridgeClass.CallStatic("setRequestObfuscationHandler", null as AndroidJavaObject);
            }

            if (responseHandler != null)
            {
                _responseProxy = new ResponseObfuscatorProxy(responseHandler);
                _bridgeClass.CallStatic("setResponseObfuscationHandler", _responseProxy);
            }
            else
            {
                _responseProxy = null;
                _bridgeClass.CallStatic("setResponseObfuscationHandler", null as AndroidJavaObject);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[BF] Failed to sync Android network obfuscation handlers: " + ex.Message);
        }
    }

    private sealed class RequestObfuscatorProxy : AndroidJavaProxy
    {
        private readonly NetworkLoggingRequestObfuscationHandler _handler;

        public RequestObfuscatorProxy(NetworkLoggingRequestObfuscationHandler handler)
            : base("com.bugfender.unity.androidlib.UnityNetworkRequestObfuscator")
        {
            _handler = handler;
        }

        public AndroidJavaObject obfuscate(string url, AndroidJavaObject headers, string body)
        {
            var managedHeaders = JavaMapToDictionary(headers);
            NetworkRequestData result;
            try
            {
                result = _handler(url ?? string.Empty, managedHeaders, body);
            }
            catch (Exception)
            {
                result = new NetworkRequestData(url ?? string.Empty, new Dictionary<string, string>(), null);
            }

            return ToJavaResult(result.Url, result.Headers, result.Body);
        }
    }

    private sealed class ResponseObfuscatorProxy : AndroidJavaProxy
    {
        private readonly NetworkLoggingResponseObfuscationHandler _handler;

        public ResponseObfuscatorProxy(NetworkLoggingResponseObfuscationHandler handler)
            : base("com.bugfender.unity.androidlib.UnityNetworkResponseObfuscator")
        {
            _handler = handler;
        }

        public AndroidJavaObject obfuscate(AndroidJavaObject headers, string body)
        {
            var managedHeaders = JavaMapToDictionary(headers);
            NetworkResponseData result;
            try
            {
                result = _handler(managedHeaders, body);
            }
            catch (Exception)
            {
                result = new NetworkResponseData(new Dictionary<string, string>(), null);
            }

            return ToJavaResult(null, result.Headers, result.Body);
        }
    }

    private static Dictionary<string, string> JavaMapToDictionary(AndroidJavaObject map)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (map == null)
        {
            return result;
        }

        try
        {
            var entrySet = map.Call<AndroidJavaObject>("entrySet");
            var iterator = entrySet.Call<AndroidJavaObject>("iterator");
            while (iterator.Call<bool>("hasNext"))
            {
                var entry = iterator.Call<AndroidJavaObject>("next");
                var keyObj = entry.Call<AndroidJavaObject>("getKey");
                var valueObj = entry.Call<AndroidJavaObject>("getValue");
                var key = keyObj != null ? keyObj.Call<string>("toString") : null;
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                result[key] = valueObj != null ? valueObj.Call<string>("toString") : string.Empty;
            }
        }
        catch (Exception)
        {
            // Fall back to empty headers.
        }

        return result;
    }

    private static AndroidJavaObject ToJavaResult(string url, IDictionary<string, string> headers, string body)
    {
        using (var javaHeaders = new AndroidJavaObject("java.util.HashMap"))
        {
            if (headers != null)
            {
                foreach (var pair in headers)
                {
                    if (pair.Key == null)
                    {
                        continue;
                    }

                    javaHeaders.Call<AndroidJavaObject>("put", pair.Key, pair.Value ?? string.Empty);
                }
            }

            return new AndroidJavaObject(
                "com.bugfender.unity.androidlib.NetworkObfuscationResult",
                url,
                javaHeaders,
                body);
        }
    }
#endif

#if UNITY_IOS && !UNITY_EDITOR
    private static void SyncIos(
        NetworkLoggingRequestObfuscationHandler requestHandler,
        NetworkLoggingResponseObfuscationHandler responseHandler)
    {
        try
        {
            if (!_iosCallbacksRegistered)
            {
                BugfenderNativeIos.RegisterNetworkRequestObfuscationCallback(OnNativeRequestObfuscation);
                BugfenderNativeIos.RegisterNetworkResponseObfuscationCallback(OnNativeResponseObfuscation);
                _iosCallbacksRegistered = true;
            }

            BugfenderNativeIos.SetNetworkLoggingRequestObfuscationHandlerEnabled(requestHandler != null);
            BugfenderNativeIos.SetNetworkLoggingResponseObfuscationHandlerEnabled(responseHandler != null);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[BF] Failed to sync iOS network obfuscation handlers: " + ex.Message);
        }
    }

    [MonoPInvokeCallback(typeof(BugfenderNativeIos.RequestObfuscationNativeDelegate))]
    private static IntPtr OnNativeRequestObfuscation(IntPtr urlPtr, IntPtr headersJsonPtr, IntPtr bodyPtr)
    {
        var url = PtrToUtf8(urlPtr) ?? string.Empty;
        var headersJson = PtrToUtf8(headersJsonPtr) ?? "{}";
        var body = bodyPtr != IntPtr.Zero ? PtrToUtf8(bodyPtr) : null;
        var headers = NetworkLoggingUtils.ParseStringMapJson(headersJson);

        var handler = NetworkLoggingManager.RequestObfuscationHandler;
        NetworkRequestData result;
        if (handler != null)
        {
            try
            {
                result = handler(url, headers, body);
            }
            catch
            {
                result = new NetworkRequestData(url, new Dictionary<string, string>(), null);
            }
        }
        else
        {
            result = new NetworkRequestData(url, headers, body);
        }

        var payload = NetworkLoggingUtils.BuildRequestObfuscationPayload(result.Url, result.Headers, result.Body);
        return AllocUtf8(payload);
    }

    [MonoPInvokeCallback(typeof(BugfenderNativeIos.ResponseObfuscationNativeDelegate))]
    private static IntPtr OnNativeResponseObfuscation(IntPtr headersJsonPtr, IntPtr bodyPtr)
    {
        var headersJson = PtrToUtf8(headersJsonPtr) ?? "{}";
        var body = bodyPtr != IntPtr.Zero ? PtrToUtf8(bodyPtr) : null;
        var headers = NetworkLoggingUtils.ParseStringMapJson(headersJson);

        var handler = NetworkLoggingManager.ResponseObfuscationHandler;
        NetworkResponseData result;
        if (handler != null)
        {
            try
            {
                result = handler(headers, body);
            }
            catch
            {
                result = new NetworkResponseData(new Dictionary<string, string>(), null);
            }
        }
        else
        {
            result = new NetworkResponseData(headers, body);
        }

        var payload = NetworkLoggingUtils.BuildResponseObfuscationPayload(result.Headers, result.Body);
        return AllocUtf8(payload);
    }

    private static string PtrToUtf8(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
        {
            return null;
        }

        var length = 0;
        while (Marshal.ReadByte(ptr, length) != 0)
        {
            length++;
        }

        if (length == 0)
        {
            return string.Empty;
        }

        var bytes = new byte[length];
        Marshal.Copy(ptr, bytes, 0, length);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }

    private static IntPtr AllocUtf8(string value)
    {
        if (value == null)
        {
            value = string.Empty;
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        var ptr = Marshal.AllocHGlobal(bytes.Length + 1);
        Marshal.Copy(bytes, 0, ptr, bytes.Length);
        Marshal.WriteByte(ptr, bytes.Length, 0);
        return ptr;
    }
#endif
}