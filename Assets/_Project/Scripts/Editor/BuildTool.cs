using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LotoSim.Editor
{
    /// <summary>Build WebGL del simulador a <c>Builds/WebGL</c>; deja el resultado en <c>Logs/build-results.txt</c>.</summary>
    public static class BuildTool
    {
        private const string ZipPath = "Builds/SimuladorLOTO-WebGL.zip";
        private const string OutputPath = "Builds/WebGL";
        private const string ResultsFile = "Logs/build-results.txt";

        [MenuItem("LotoSim/Build/WebGL")]
        public static void BuildWebGL() => EditorApplication.delayCall += Build;

        /// <summary>Zip listo para subir a itch.io ("This file will be played in the browser").</summary>
        private static string CreateItchZip()
        {
            if (File.Exists(ZipPath))
                File.Delete(ZipPath);
            System.IO.Compression.ZipFile.CreateFromDirectory(OutputPath, ZipPath, System.IO.Compression.CompressionLevel.Optimal, false);
            return $"{ZipPath} ({new FileInfo(ZipPath).Length / (1024 * 1024)} MB)";
        }

        private static void Build()
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(ResultsFile, $"BUILD INICIO {System.DateTime.Now:HH:mm:ss}\n");
            InputActionsGuard.Ensure();
            PlayerSettings.WebGL.template = "PROJECT:LotoSim";

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/_Project/Scenes/Menu.unity", "Assets/_Project/Scenes/PumpRoom.unity" },
                locationPathName = OutputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            // (el zip para itch.io se arma abajo; index.html debe quedar en la raíz del zip)
            var summary = report.summary;
            string zipInfo = string.Empty;
            if (summary.result == BuildResult.Succeeded)
                zipInfo = $" zip={CreateItchZip()}";
            File.AppendAllText(ResultsFile,
                $"BUILD FIN result={summary.result} errores={summary.totalErrors} avisos={summary.totalWarnings} " +
                $"tamaño={summary.totalSize / (1024 * 1024)} MB tiempo={summary.totalTime.TotalSeconds:0}s{zipInfo} {System.DateTime.Now:HH:mm:ss}\n");
        }
    }
}
