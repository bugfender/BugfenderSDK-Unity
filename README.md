# Bugfender SDK for Unity

Unity bindings for the native Bugfender iOS and Android SDKs.

This package requires Unity 2022.3.27 (LTS), Unity 6 or newer.

## Installation

To use Bugfender in your Unity project, follow these steps:
 * Browse to the Package Manager panel (**Window** menu > **Package Manager**)
 * Press the **Add (+)** button and **Add package from git URL...**
 * Enter the URL: `https://github.com/bugfender/BugfenderSDK-Unity.git#2.0.0`
 * From the **Project** panel, open the **Packages** / **Bugfender** package and drag the **Bugfender** prefab to your main scene. 
 * Set the app key in the **Inspector** panel.
 * You can use the `Bugfender.Log()` method to write logs.

If your game has several scenes, you only need to add Bugfender once, on the first scene that gets executed.

If you use Bugfender in a platform other than iOS or Android, any calls to Bugfender will be ignored. Other than that, your application will keep working as usual. Please note the WebGL platform is not supported yet, although we plan to support it in the future.

### Script Execution Order
You want the Bugfender SDK to initialize early to capture as many logs and errors as possible. Therefore, you can tweak the execution order to make sure it runs before other scripts. By default, it starts with priority `-1` (lowest runs first).

You can change the priority in the **Project Settings** > **Script Execution Order**.

### Adjust the native Bugfender SDK versions
This package imports the native Bugfender SDKs for iOS and Android using Swift Package Manager and Gradle.

By default, the latest compatible versions are used. If you would like to tweak that, you can fork this project and edit these files:

* For iOS: `Editor/IOSProjectBuildCustomizer.cs`
* For Android: `Runtime/Plugins/Android/Bugfender.androidlib/build.gradle`

## Example project
Check out this proejct to see Bugfender in action: https://github.com/bugfender/unity-demo
