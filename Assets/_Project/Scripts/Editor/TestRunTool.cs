using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace LotoSim.Editor
{
    /// <summary>
    /// Lanza los tests del proyecto desde un menú o por código y deja el resultado en
    /// <c>Logs/test-results.txt</c> (sobrevive al domain reload de Play Mode).
    /// </summary>
    [InitializeOnLoad]
    public static class TestRunTool
    {
        private const string ResultsFile = "Logs/test-results.txt";
        private static readonly TestRunnerApi Api = ScriptableObject.CreateInstance<TestRunnerApi>();

        static TestRunTool() => Api.RegisterCallbacks(new Logger());

        [MenuItem("LotoSim/Tests/Correr Edit Mode")]
        public static void RunEditMode() => Run(TestMode.EditMode);

        [MenuItem("LotoSim/Tests/Correr Play Mode")]
        public static void RunPlayMode() => Run(TestMode.PlayMode);

        public static void Run(TestMode mode)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(ResultsFile, $"RUN {mode} {System.DateTime.Now:HH:mm:ss}\n");
            // Ojo: si hay una escena con cambios sin guardar, el Test Runner abre el diálogo
            // "Scene(s) Have Been Modified" y bloquea el editor hasta que se responda.
            if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().isDirty)
                UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Api.Execute(new ExecutionSettings(new Filter { testMode = mode }));
        }

        private sealed class Logger : ICallbacks
        {
            private int _passed, _failed;
            private readonly StringBuilder _failures = new();

            public void RunStarted(ITestAdaptor testsToRun)
            {
                _passed = _failed = 0;
                _failures.Clear();
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                File.AppendAllText(ResultsFile,
                    $"{_failures}FIN passed={_passed} failed={_failed} status={result.TestStatus} {System.DateTime.Now:HH:mm:ss}\n");
            }

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.HasChildren)
                    return;
                if (result.TestStatus == TestStatus.Passed)
                {
                    _passed++;
                }
                else
                {
                    _failed++;
                    _failures.AppendLine($"FAIL {result.FullName}: {result.Message?.Replace('\n', ' ')}");
                }
            }
        }
    }
}
