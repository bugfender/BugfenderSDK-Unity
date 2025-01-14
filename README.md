# Bugfender SDK for Unity

Unity bindings for the native Bugfender iOS and Android SDKs.

This package requires Unity 2022.3.27 (LTS), Unity 6 or newer.

## Installation

To use Bugfender in your Unity project, follow these steps:
 * Browse to the Package Manager panel (**Window** menu > **Package Manager**)
 * Press the **Add (+)** button and **Add package from git URL...**
 * Enter the URL: <code>https://github.com/bugfender/BugfenderSDK-Unity.git#2.0.0-alpha.0</code>
 * From the **Project** panel, drag the **Packages** / **Bugfender** / **Bugfender** prefab to your main scene. 
 * Set the app key in the **Inspector** panel.
 * You can use the <code>Bugfender.Log()</code> method to write logs.

If your game has several scenes, you only need to add Bugfender once, on the first scene that gets executed.

If you use Bugfender in a platform other than iOS or Android, any calls to Bugfender will be just ignored but other than that, your application will keep working as usual. Please note the WebGL platform is currently not supported yet, although we plan to support it in the future.

### Script Execution Order
You want the Bugfender SDK to initialize early to capture as many logs and errors as possible. Therefore, you can tweak the execution order to make sure it runs before other scripts. By default, it starts with priority <code>-1</code> (lowest runs first).

You can change the priority in the **Project Settings** > **Script Execution Order**:

![Script Execution Order](https://user-images.githubusercontent.com/864706/220148469-91455103-32d7-46fc-be27-c718f86dc930.png)

## How to update the libraries

To update the Android library:
 * Download the [Bugfender Android SDK aar file](https://central.sonatype.com/artifact/com.bugfender.sdk/android/versions) (click on **Browse**, then download the file)
 * Delete the current file in `Plugins/Android` and replace it with the .aar file just downloaded

To update the iOS framework: 
 * Download the latest [Bugfender iOS SDK release](https://github.com/bugfender/BugfenderSDK-iOS/releases): `BugfenderSDK.xcframework.zip`
 * Unzip it
 * Under `Plugins/iOS`, delete the current `BugfenderSDK.xcframework` and replace it with the one you just downloaded (keep `BugfenderBridge.mm`)
 * Edit the `BugfenderSDK.xcframework` item and, under the **iOS Platform settings**, add the following **Framework Dependencies**: `MobileCoreServices` and `Security`; and check **Add to Embedded Binaries**.
