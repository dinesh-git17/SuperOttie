using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace SuperOttie.Editor
{
    /// <summary>
    /// Pins Xcode build settings that break Unity projects when Xcode's "Update to recommended settings"
    /// turns them on: user script sandboxing blocks the IL2CPP run-script phase from loading its tools,
    /// and the module verifier rejects UnityFramework's umbrella header.
    /// Also declares that the app uses no non-exempt encryption, so TestFlight builds skip the
    /// export-compliance question.
    /// </summary>
    public static class XcodeProjectFixups
    {
        static readonly (string key, string value)[] Settings =
        {
            ("ENABLE_USER_SCRIPT_SANDBOXING", "NO"),
            ("ENABLE_MODULE_VERIFIER", "NO"),
        };

        [PostProcessBuild(100)]
        public static void OnPostprocessBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;
            string projPath = PBXProject.GetPBXProjectPath(path);
            var proj = new PBXProject();
            proj.ReadFromFile(projPath);
            foreach (string guid in new[] { proj.ProjectGuid(), proj.GetUnityMainTargetGuid(), proj.GetUnityFrameworkTargetGuid(), proj.TargetGuidByName("GameAssembly") })
            {
                if (string.IsNullOrEmpty(guid)) continue;
                foreach (var (key, value) in Settings) proj.SetBuildProperty(guid, key, value);
            }
            File.WriteAllText(projPath, proj.WriteToString());

            string plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            plist.WriteToFile(plistPath);
        }
    }
}
