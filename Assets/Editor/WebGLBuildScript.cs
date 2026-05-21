using UnityEditor;
using System;
using UnityEngine;

public class WebGLBuildScript
{
    public static void Build()
    {
        Debug.Log("WebGLBuildScript: Build started...");
        string[] scenes = { "Assets/Scenes/SampleScene.unity" };
        string buildPath = "Build_WebGL";
        
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.WebGL;
        buildPlayerOptions.options = BuildOptions.None;
        
        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;
        
        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"WebGLBuildScript: Build succeeded! Size: {summary.totalSize} bytes");
        }
        else if (summary.result == UnityEditor.Build.Reporting.BuildResult.Failed)
        {
            Debug.LogError($"WebGLBuildScript: Build failed! Errors: {summary.totalErrors}");
            throw new Exception("WebGL Build Failed!");
        }
    }
}
