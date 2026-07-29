# Bugfender SDK for Unity

Unity bindings for the native Bugfender iOS and Android SDKs.

This package requires Unity 2022.3.27 (LTS), Unity 6 or newer.

## Installation

To use Bugfender in your Unity project, follow these steps:
 * Browse to the Package Manager panel (**Window** menu > **Package Manager**)
 * Press the **Add (+)** button and **Add package from git URL...**
 * Enter the URL: `https://github.com/bugfender/BugfenderSDK-Unity.git`
 * From the **Project** panel, open the **Packages** / **Bugfender** package and drag the **Bugfender** prefab to your main scene. 
 * Set the app key in the **Inspector** panel.
 * You can use the `Bugfender.Log()` method to write logs.

If your game has several scenes, you only need to add Bugfender once, on the first scene that gets executed.

If you use Bugfender on a platform other than iOS or Android, any calls to Bugfender will be ignored. Other than that, your application will keep working as usual. Please note that the WebGL platform is not yet supported, although we plan to support it in the future.

### Script Execution Order
You want the Bugfender SDK to initialize early to capture as many logs and errors as possible. Therefore, you can tweak the execution order to ensure it runs before other scripts. It starts with priority `-1` (lowest runs first) by default.

You can change the priority in the **Project Settings** > **Script Execution Order**.

### Network logging

Network logging is **opt-in** and disabled by default. When enabled, HTTP requests can appear in the Bugfender dashboard as `bf_network` logs.

```csharp
Bugfender.SetNetworkLoggingEnabled(true);
// Optional:
Bugfender.SetNetworkLoggingCaptureBodies(false);
Bugfender.SetNetworkLoggingCaptureErrorResponseBodies(true);
Bugfender.SetNetworkLoggingURLFilter(
    allowlist: new[] { "https://api.example.com/*" },
    denylist: new[] { "*/secrets/*" });
Bugfender.SetNetworkLoggingMaxRequestsPerMinute(60);

// Redact secrets before logs are sent (also applied to native OkHttp / URLSession capture):
Bugfender.SetNetworkLoggingRequestObfuscationHandler((url, headers, body) =>
{
    if (headers.ContainsKey("Authorization"))
    {
        headers["Authorization"] = "***";
    }
    return new NetworkRequestData(url, headers, body);
});
Bugfender.SetNetworkLoggingResponseObfuscationHandler((headers, body) =>
{
    return new NetworkResponseData(headers, body);
});
```

Unity does not have a single global HTTP stack. Instrument traffic with one of:

**`HttpClient`** — wrap with `BugfenderHttpMessageHandler`:

```csharp
var client = new HttpClient(new BugfenderHttpMessageHandler());
var response = await client.GetAsync("https://api.example.com/users");
```

**`UnityWebRequest`** — use the helpers:

```csharp
var request = UnityWebRequest.Get("https://api.example.com/users");
yield return BugfenderUnityWebRequest.Send(request);
```

Or manually: `BugfenderUnityWebRequest.Prepare(request)` before send, then `Complete(...)` after.

**Other HTTP libraries** — build a `NetworkLogEntry` and call `Bugfender.LogNetwork(entry)`.

When network logging is enabled, instrumented requests also receive correlation headers:
`X-Bugfender-Session-ID` and `X-Bugfender-Request-ID`.

On Android/iOS the same config APIs (including obfuscation handlers) are forwarded to the native SDKs (OkHttp / URLSession). Typical Unity game traffic still needs the C# helpers above. Android ships `android-okhttp`; native OkHttp clients must add `BugfenderOkHttpInterceptor` (and optionally `BugfenderOkHttpEventListenerFactory`) themselves.

### Adjust the native Bugfender SDK versions
This package imports the native Bugfender SDKs for iOS and Android using Swift Package Manager and Gradle.

By default, the latest compatible versions are used. If you would like to tweak that, you can fork this project and edit these files:

* For iOS: `Editor/IOSProjectBuildCustomizer.cs` (SPM `3.0.1`+)
* For Android: `Runtime/Plugins/Android/Bugfender.androidlib/build.gradle` (`com.bugfender.sdk:android:4.+` and `android-okhttp:4.+`)

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history.

## Example project
Check out this project to see Bugfender in action: https://github.com/bugfender/unity-demo

This package also includes a **Network Logging** sample (Package Manager → Bugfender → Samples) that configures capture, filters, rate limits, and obfuscation handlers for `HttpClient` and `UnityWebRequest`.

## Testing
See `TESTING.md` for a manual validation checklist for Unity import, Android builds, iOS builds, and runtime verification.
