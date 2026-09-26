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
    /// export-compliance question, and aligns the embedded frameworks' MinimumOSVersion with the
    /// deployment target they are linked against (Unity ships UnityRuntime's Info.plist at 15.0, and
    /// App Store Connect rejects the mismatch as ITMS-90208).
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

            string minimumOS = PlayerSettings.iOS.targetOSVersionString;
            string frameworks = Path.Combine(path, "Frameworks");
            if (!Directory.Exists(frameworks)) return;
            foreach (string framework in Directory.GetDirectories(frameworks, "*.framework"))
            {
                string frameworkPlist = Path.Combine(framework, "Info.plist");
                if (!File.Exists(frameworkPlist)) continue;
                var info = new PlistDocument();
                info.ReadFromFile(frameworkPlist);
                info.root.SetString("MinimumOSVersion", minimumOS);
                info.WriteToFile(frameworkPlist);
            }
        }
    }
}
