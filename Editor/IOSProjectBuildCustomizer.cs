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

        // Add the Remote Package to the Xcode project
        pbxProject.AddRemotePackageFrameworkToProject(pbxProject.GetUnityFrameworkTargetGuid(), "BugfenderLibrary", packageGuid, false /* required dependency */);
      
        // Apply changes to the pbxproj file
        pbxProject.WriteToFile(projectPath);
    }
}