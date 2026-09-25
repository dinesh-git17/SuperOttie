using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SuperOttie.Editor
{
    /// <summary>
    /// Headless iOS builds. Produces an Xcode project that is then compiled with xcodebuild:
    /// <c>unity build . --target iOS --execute-method SuperOttie.Editor.BuildScript.BuildIOSSimulator</c>
    /// </summary>
    public static class BuildScript
    {
        public const string SimulatorOutput = "Builds/iOS-Simulator";
        public const string DeviceOutput = "Builds/iOS-Device";

        [MenuItem("Super Ottie/Build/iOS Simulator Xcode Project")]
        public static void BuildIOSSimulator() => Build(iOSSdkVersion.SimulatorSDK, SimulatorOutput);

        [MenuItem("Super Ottie/Build/iOS Device Xcode Project")]
        public static void BuildIOSDevice() => Build(iOSSdkVersion.DeviceSDK, DeviceOutput);

        static void Build(iOSSdkVersion sdk, string output)
        {
            try
            {
                ProjectSetup.Run();
                PlayerSettings.iOS.sdkVersion = sdk;
                EditorUserBuildSettings.development = false;
                var options = new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                    locationPathName = output,
                    target = BuildTarget.iOS,
                    targetGroup = BuildTargetGroup.iOS,
                    options = BuildOptions.None,
                };
                var report = BuildPipeline.BuildPlayer(options);
                var summary = report.summary;
                Debug.Log($"[Build] {summary.result}: {summary.totalErrors} errors, {summary.totalWarnings} warnings, {summary.totalTime}");
                if (summary.result != BuildResult.Succeeded) Fail($"Build {summary.result}");
                else if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Fail(e.Message);
            }
        }

        static void Fail(string message)
        {
            Debug.LogError("[Build] " + message);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
