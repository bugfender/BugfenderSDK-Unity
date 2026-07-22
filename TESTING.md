# Testing Bugfender SDK for Unity

This document describes the manual validation flow for the Unity SDK package.

## Test project

Use the sample project at:

`/home/aleix/Projects/bugfender/unity-demo`

To test local package changes, the sample project's package manifest should point to:

`file:../../BugfenderSDK-Unity`

## Unity version

Validate with Unity `6000.3.10f` or the target Unity 6 version under support.

## Import checks

1. Open `/home/aleix/Projects/bugfender/unity-demo` in Unity.
2. Wait for Package Manager and asset import to finish.
3. Confirm the Bugfender package resolves from the local file path.
4. Confirm there are no C# compilation errors.
5. Confirm there are no Android plugin import errors for `Bugfender.androidlib`.
6. Confirm there are no iOS plugin compilation issues for `BugfenderBridge.mm`.

## Android build checks

1. Switch the active platform to Android.
2. Build the sample project.
3. If the build fails, capture the first Gradle error block.

Expected result:

- No `Could not find com.bugfender.sdk:android` errors.
- No `ClassNotFoundException` or `cannot find symbol` errors for `com.bugfender.sdk.Bugfender`.
- No missing module or `Project with path` errors involving `Bugfender.androidlib`.

## iOS build checks

1. Switch the active platform to iOS.
2. Build the Xcode project from Unity.
3. Open the generated Xcode project and build it.

Expected result:

- Swift Package Manager resolves `BugfenderSDK-iOS`.
- No compile or link errors for `Bugfender`.
- No selector or symbol errors related to `setSDKType:version:`.

## Runtime checks

1. Set a valid Bugfender app key in the sample scene.
2. Run the app on a device or simulator.
3. Confirm startup completes without crashing.
4. Confirm Bugfender initializes successfully.
5. Send a test log and verify it appears in Bugfender.

## Network logging checks

1. Call `Bugfender.SetNetworkLoggingEnabled(true)` after init.
2. Send an HTTP request with either:
   - `new HttpClient(new BugfenderHttpMessageHandler())`, or
   - `yield return BugfenderUnityWebRequest.Send(request)`
3. Confirm a log with tag `bf_network` appears in the device session.
4. Confirm the JSON payload includes `url`, `method`, `request_id`, `start_time`, `duration_ms`, `request_headers`, `response_headers`, and `timing`.
5. Confirm instrumented requests include `X-Bugfender-Request-ID` (and `X-Bugfender-Session-ID` when a session id is available).
6. Optional: enable body capture / error-body capture and verify bodies appear only as configured.
7. Optional: set an allowlist/denylist and rate limit and verify filtering.

## What to capture on failure

When reporting a failure, include:

- Unity version
- Target platform
- Whether the failure happens during import, build, or runtime
- The first relevant error block from Unity Console, Gradle, or Xcode
