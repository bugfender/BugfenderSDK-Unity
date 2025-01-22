using UnityEditor;
using System.IO;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public class IOSProjectBuildCustomizer  
{
    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget buildTarget, string pathToBuiltProject)
    {

        // Stop processing if build target is not iOS
        if (buildTarget != BuildTarget.iOS)
            return;

        // Initialize PBXProject
        string projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        PBXProject pbxProject = new PBXProject();
        pbxProject.ReadFromFile(projectPath);

        // Get the GUID you want to add the package to
        string mainTargetGuid = pbxProject.GetUnityMainTargetGuid();

        // Get the remote package GUID
        string packageGuid = pbxProject.AddRemotePackageReferenceAtVersionUpToNextMajor("https://github.com/bugfender/BugfenderSDK-iOS", "2.0.0");

        // Add the Remote Package to the Xcode project (both Unity framework and main target)
        pbxProject.AddRemotePackageFrameworkToProject(pbxProject.GetUnityFrameworkTargetGuid(), "BugfenderLibrary", packageGuid, false /* required dependency */);
        pbxProject.AddRemotePackageFrameworkToProject(pbxProject.GetUnityMainTargetGuid(), "BugfenderLibrary", packageGuid, false /* required dependency */);
        pbxProject.AddFrameworkToProject(mainTargetGuid, "SystemConfiguration.framework", false);
        pbxProject.AddFrameworkToProject(mainTargetGuid, "Security.framework", false);
        pbxProject.AddFrameworkToProject(mainTargetGuid, "MobileCoreServices.framework", false);
        pbxProject.AddFrameworkToProject(mainTargetGuid, "CoreGraphics.framework", false);
        pbxProject.AddFrameworkToProject(mainTargetGuid, "libc++.tbd", false);
      
        // Apply changes to the pbxproj file
        pbxProject.WriteToFile(projectPath);
    }
}