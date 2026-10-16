using UnityEditor;
using UnityEditor.SceneManagement;

namespace LotoSim.Editor
{
    /// <summary>Atajos de menú para abrir las escenas del proyecto.</summary>
    public static class SceneShortcuts
    {
        private const string ScenesPath = "Assets/_Project/Scenes/";

        [MenuItem("LotoSim/Escenas/Menu", priority = 0)]
        private static void OpenMenu() => Open("Menu");

        [MenuItem("LotoSim/Escenas/PumpRoom", priority = 1)]
        private static void OpenPumpRoom() => Open("PumpRoom");

        private static void Open(string sceneName)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene($"{ScenesPath}{sceneName}.unity");
        }
    }
}
