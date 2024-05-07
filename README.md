# BugfenderSDK-Unity

Bugfender is compatible with Unity iOS and Android platforms. We have made an asset package to adapt the native Bugfender iOS and Android SDKs to the interface required by Unity.

This package requires Unity 2022.3.27 (LTS), Unity 2023.2.18, or newer.

To use Bugfender in your Unity project, follow these steps:
 * Download and import the assets package into your project.
 * Drag the Assets / Bugfender / Prefabs / Bugfender prefab to your hierarchy. 
 * Select Bugfender GameObject and set the Bugfender app key in the Inspector panel.
 * If your game has several scenes, you only need to add Bugfender once, on the first scene that gets executed.
 * You can use the Bugfender.Log() method to write logs.

If you use Bugfender in a platform other than iOS or Android, any calls to Bugfender will be just ignored but other than that, your application will keep working as usual. Please note the WebGL platform is currently not supported yet, although we plan to support it in the future.

### Script Execution Order
Bugfender is provided as a Prefab that you can drop into your project. Before using Bugfender, the `Start()` method of the Prefab needs to be called, so you can ensure it's available before your scripts start running by setting a **Script Execution Order** in your **Project Settings**, like so:

![Script Execution Order](https://user-images.githubusercontent.com/864706/220148469-91455103-32d7-46fc-be27-c718f86dc930.png)

## How to update the libraries

To update the Android library:
 * Download the [Bugfender Android SDK aar file](https://central.sonatype.com/artifact/com.bugfender.sdk/android/versions) (click on **Browse**, then download the file)
 * Delete the current file in `Assets/Plugins/Android` and replace it with the .aar file just downloaded

To update the iOS framework: 
 * Download the latest [Bugfender iOS SDK release](https://github.com/bugfender/BugfenderSDK-iOS/releases): `BugfenderSDK.xcframework.zip`
 * Unzip it
 * Under `Assets/Plugins/iOS`, delete the current `BugfenderSDK.xcframework` and replace it with the one you just downloaded (keep `BugfenderBridge.mm`)
 * Edit the `BugfenderSDK.xcframework` item and, under the **iOS Platform settings**, add the following **Framework Dependencies**: `MobileCoreServices` and `Security`; and check **Add to Embedded Binaries**.

## Exporting the `.unitypackage`

 * Run `./export.sh`
