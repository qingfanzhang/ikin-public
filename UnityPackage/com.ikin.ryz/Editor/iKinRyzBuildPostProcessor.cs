using System.IO;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
using UnityEngine;

class ikinRyzBuildPostProcessor : IPostprocessBuildWithReport
{
#if DEBUG
	private static string ToString(BuildFile buildFile, int indentDepth = 0)
    {
        string indent = new string('\t', indentDepth);

        return string.Format("{0}Path: {1},\n{0}Role:{2},\n{0}ID:{3},\n{0}Size:{4}\n", indent, buildFile.path, buildFile.role, buildFile.id, buildFile.size);
    }

    private static string ToString(BuildFile[] buildFiles, int indentDepth = 0)
    {
        string indent = new string('\t', indentDepth);

        string[] x = buildFiles.Select(y => "Build File:\n" + ToString(y, indentDepth + 1)).ToArray();

        string z = string.Join(",\n", x);

        return string.Format("{0}{{\n {1} }}\n", indent, z);
    }

    private static string ToString(BuildSummary buildSummary, int indentDepth = 0)
    {
        string indent = new string('\t', indentDepth);

        return string.Format("{0}GUID:{1},\n{0}Output Path:{2},\n{0}Platform:{3},\n{0}PlatformGroup:{4}\n{0}Result:{5}\n{0}Options:{6}\n",
            indent, buildSummary.guid, buildSummary.outputPath, buildSummary.platform, buildSummary.platformGroup, buildSummary.result, buildSummary.options);
    }

    private static string ToString(BuildReport buildReport, int indentDepth = 0)
    {
        string indent = new string('\t', indentDepth);
        int nextIndent = indentDepth + 1;

        return string.Format("{0}Report Name:{1}\n{0}Report Name:{2}\n{0}Build Files:{3}\n", indent, buildReport.name, ToString(buildReport.summary, nextIndent), ToString(buildReport.files, nextIndent));
    }
#endif

    public void OnPostprocessBuild(BuildReport buildReport)
    {
#if UNITY_IOS
        // Get the location of the iOS pbxproj file that was generated during the build.
        string projectPath = buildReport.summary.outputPath + "/Unity-iPhone.xcodeproj/project.pbxproj";

        var pbxProject = new PBXProject();

        // Parse the file into this type.
        pbxProject.ReadFromFile(projectPath);

        // Get the UnityFramework package in the XCode project.
        string targetGuid = pbxProject.GetUnityFrameworkTargetGuid();

        // Add the frameworks that the iKin Ryz plugin needs.
        pbxProject.AddFrameworkToProject(targetGuid, "IOSurface.framework", false);

        // Request the modified file contents, and write to file.
        File.WriteAllText(projectPath, pbxProject.WriteToString());

#if DEBUG
        Debug.Log(string.Format("iKinRyzBuildPostProcessor.OnPostprocessBuild\nBuild Report:\n{0}", ToString(buildReport)));
#endif
#endif
    }

	public int callbackOrder
	{
		get
		{
			return 0;
		}
	}
}
