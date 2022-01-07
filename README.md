# BugfenderSDK-Unity

Bugfender is compatible with Unity iOS and Android platforms. We have made an asset package to adapt the native iOS and Android to the interface required by Unity.
 
To use Bugfender in your Unity project, follow these steps:
 * Download and import the assets package into your project.
 * Drag the Assets / Bugfender / Prefabs / Bugfender prefab to your hierarchy. 
 * Select Bugfender GameObject and set the Bugfender app key in the Inspector panel.
 * If your game has several scenes, you only need to add Bugfender once, on the first scene that gets executed.
 * You can use the Bugfender.Log() method to write logs.

If you use Bugfender in a platform other than iOS or Android, any calls to Bugfender will be just ignored but other than that, your application will keep working as usual. Please note the WebGL platform is currently not supported yet, although we plan to support it in the future.
 
## How to update the libraries

To update the Android library:
 * Download the Bugfender Android SDK aar file
 * Replace it in your project in Assets/Plugins/Android 

To update the iOS framework: 
 * Download the latest Bugfender iOS SDK release: static-lib.zip
 * Unzip it
 * Replace it in your project in Assets/Plugins/iOS 
