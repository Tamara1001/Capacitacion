using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LotoSim.Editor
{
    /// <summary>Build WebGL del simulador a <c>Builds/WebGL</c>; deja el resultado en <c>Logs/build-results.txt</c>.</summary>
    public static class BuildTool
    {
        private const string OutputPath = "Builds/WebGL";
        private const string ResultsFile = "Logs/build-results.txt";

        [MenuItem("LotoSim/Build/WebGL")]
        public static void BuildWebGL() => EditorApplication.delayCall += Build;

        private static void Build()
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(ResultsFile, $"BUILD INICIO {System.DateTime.Now:HH:mm:ss}\n");
            InputActionsGuard.Ensure();

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/_Project/Scenes/Menu.unity", "Assets/_Project/Scenes/PumpRoom.unity" },
                locationPathName = OutputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            File.AppendAllText(ResultsFile,
                $"BUILD FIN result={summary.result} errores={summary.totalErrors} avisos={summary.totalWarnings} " +
                $"tamaño={summary.totalSize / (1024 * 1024)} MB tiempo={summary.totalTime.TotalSeconds:0}s {System.DateTime.Now:HH:mm:ss}\n");
        }
    }
}
