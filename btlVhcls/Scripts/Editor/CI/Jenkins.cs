#if UNITY_EDITOR
using System;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class Jenkins
{
    static string[] SCENES = FindEnabledEditorScenes();

    static string APP_NAME = "IronTanks";
    static string TARGET_DIR = ".";

//	[MenuItem("Custom/CI/Build Mac OS X")]
//	static void PerformMacOSXBuild()
//	{
//		string target_dir = APP_NAME + ".app";
//		GenericBuild(SCENES, TARGET_DIR + "/" + target_dir, BuildTarget.StandaloneOSXIntel, BuildOptions.None);
//	}

    [MenuItem("HelpTools/CI/Build Android")]
    static void PerformAndroidBuild()
    {
        string target_dir = APP_NAME + ".apk";
        PlayerSettings.keystorePass = "debugkey";
        PlayerSettings.keyaliasPass = "debugkey";
        GenericBuild (SCENES, TARGET_DIR + "/" + target_dir, BuildTargetGroup.Android, BuildTarget.Android, BuildOptions.None);
    }

    private static string[] FindEnabledEditorScenes()
    {
        var EditorScenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes) {
            if (!scene.enabled)
                continue;
            EditorScenes.Add(scene.path);
        }
        return EditorScenes.ToArray();
    }

    static void GenericBuild(string[] scenes, string target_dir, BuildTargetGroup buid_group, BuildTarget build_target, BuildOptions build_options)
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(buid_group, build_target);
        string res = BuildPipeline.BuildPlayer(scenes, target_dir, build_target, build_options);
        if (res.Length > 0) {
            throw new Exception("BuildPlayer failure: " + res);
        }
    }
}
#endif