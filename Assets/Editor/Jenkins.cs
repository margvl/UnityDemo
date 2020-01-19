using System;
using Boo.Lang;
using UnityEditor;
using UnityEditor.Build.Reporting;

class Jenkins {
    private class BuildConfiguration {
        public readonly string[] Scenes;
        public readonly string AppName;
        public readonly string OutputPath;

        public BuildConfiguration(string[] scenes, string appName, string outputPath) {
            Scenes = scenes;
            AppName = appName;
            OutputPath = outputPath;
        }
    }
    
    public static void PerformIOSBuild() {
        BuildPlayerOptions options = GetIOSBuildOptions();
        PerformBuild(options, BuildOptions.None);
    }

    public static void PerformAndroidBuild() {
        BuildPlayerOptions options = GetAndroidBuildOptions();
        PerformBuild(options, BuildOptions.None);
    }
    
    private static void PerformBuild(BuildPlayerOptions options, BuildOptions buildOptions) {
        PrintBuildOptions(options);

        // https://docs.unity3d.com/ScriptReference/EditorUserBuildSettings.SwitchActiveBuildTarget.html
        bool switchResult = EditorUserBuildSettings.SwitchActiveBuildTarget(
                options.targetGroup, 
                options.target);
        
        if (!switchResult) {
            Console.WriteLine("ERROR! Unable to change Build Target to: " + options.target);
            return;
        }
        
        Console.WriteLine("NOTE! Successfully changed Build Target to: " + options.target);
        BuildReport report = BuildPipeline.BuildPlayer(options);
        PrintBuildReport(report);
        
        // https://docs.unity3d.com/ScriptReference/BuildPipeline.BuildPlayer.html
        BuildReport buildReport = BuildPipeline.BuildPlayer(
                options.scenes, 
                options.locationPathName, 
                options.target,
                buildOptions);
        BuildSummary buildSummary = buildReport.summary;
        if (buildSummary.result == BuildResult.Succeeded){
            Console.WriteLine("SUCCESS! Time:" + buildSummary.totalTime + " Size:" + buildSummary.totalSize + " bytes");
        } else {
            Console.WriteLine("ERROR! Build Failed: Time:" + buildSummary.totalTime + " Total Errors:" + buildSummary.totalErrors);
        }
    }
    
    private static BuildPlayerOptions GetAndroidBuildOptions() {
        BuildConfiguration configuration = GetBuildConfiguration();
        BuildPlayerOptions options = new BuildPlayerOptions {
                scenes = configuration.Scenes,
                locationPathName = configuration.OutputPath + "/android/" + configuration.AppName + ".apk",
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development
        };

        return options;
    }

    private static BuildPlayerOptions GetIOSBuildOptions() {
        BuildConfiguration configuration = GetBuildConfiguration();
        BuildPlayerOptions options = new BuildPlayerOptions {
                scenes = configuration.Scenes,
                locationPathName = configuration.OutputPath + "/ios/project",
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.Development
        };

        return options;
    }

    private static BuildConfiguration GetBuildConfiguration() {
        string[] argList = Environment.GetCommandLineArgs();
        if (argList.Length == 0) {
            Console.WriteLine("ERROR! Unable to get command line arguments");
            return null;
        }

        string appName = "";
        string outputPath = "";
        for (int i = 0; i < argList.Length; i++) {
            if (argList[i] == "-executeMethod") {
                if (i + 4 < argList.Length) {
                    // BuildMacOS method is args[i+1]
                    appName = argList[i + 2];
                    outputPath = argList[i + 3];
                    i += 3;
                } else {
                    Console.WriteLine("ERROR! Incorrect parameters for -executeMethod " +
                                      "Expected format: -executeMethod <method name> <app name> <output path>");
                    return null;
                }
            } else {
                Console.WriteLine("ERROR! Missing command line argument -executeMethod");
            }
        }

        return new BuildConfiguration(
                GetScenes(), 
                appName, 
                GetWorkspacePath() + "/" + outputPath);
    }
    
    private static string[] GetScenes() {
        List<string> editorSceneList = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes) {
            if (scene.enabled) {
                editorSceneList.Add(scene.path);
            }
        }
        return editorSceneList.ToArray();
    }

    private static string GetWorkspacePath() {
        string path = Environment.GetEnvironmentVariable("WORKSPACE");
        if (path == null) {
            throw new Exception("Could not access environment variable: WORKSPACE");
        }
        return path;
    }

    private static void PrintBuildOptions(BuildPlayerOptions options) {
        Console.WriteLine("+---------------------------------------------+");
        Console.WriteLine("|                Build Options                |");
        Console.WriteLine("+---------------------------------------------+");
        Console.WriteLine("| Target: " + options.target);
        Console.WriteLine("| Target Group: " + options.targetGroup);
        Console.WriteLine("| Options: " + options.options);
        Console.WriteLine("| Scene Count: " + options.scenes.Length);
        Console.WriteLine("| Output Path: " + options.locationPathName);
        Console.WriteLine("+---------------------------------------------+");
    }

    private static void PrintBuildReport(BuildReport report) {
        if (report.summary.result != BuildResult.Succeeded) {
            Console.WriteLine("+---------------------------------------------+");
            Console.WriteLine("|                Build Failure                |");
            Console.WriteLine("+---------------------------------------------+");
            Console.WriteLine("| " + report.name);
            Console.WriteLine("| Platform: " + report.summary.platform);
            Console.WriteLine("| Platform Group: " + report.summary.platformGroup);
            Console.WriteLine("| Total Warnings " + report.summary.totalWarnings);
            Console.WriteLine("| Total Errors " + report.summary.totalErrors);
            Console.WriteLine("| Result: " + report.summary.result);
            Console.WriteLine("+---------------------------------------------+");

            throw new Exception("Build Failure");
        }
    }
}