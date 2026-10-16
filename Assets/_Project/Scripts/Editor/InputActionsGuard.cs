using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LotoSim.Editor
{
    /// <summary>
    /// Garantiza la configuración de entrada del proyecto:
    /// <list type="bullet">
    /// <item><c>LotoSim.inputactions</c> es el asset global de acciones (<c>InputSystem.actions</c>) y está en los
    /// Preloaded Assets. Sin esto el runtime devuelve null y WASD, mouse e interacción dejan de funcionar
    /// (en el Editor y, sobre todo, en la build WebGL).</item>
    /// <item>Existe un asset de <c>InputSettings</c> propio donde, dentro del Editor, el teclado y el mouse
    /// llegan siempre a la Game view aunque no tenga el foco de ventana. Con el valor por defecto
    /// (<c>PointersAndKeyboardsRespectGameViewFocus</c>) el teclado puede dejar de responder mientras el
    /// mouse capturado sigue moviendo la cámara.</item>
    /// </list>
    /// </summary>
    [InitializeOnLoad]
    public static class InputActionsGuard
    {
        private const string ActionsPath = "Assets/_Project/Input/LotoSim.inputactions";
        private const string SettingsPath = "Assets/_Project/Input/LotoSim.inputsettings.asset";

        static InputActionsGuard() => EditorApplication.delayCall += Ensure;

        [MenuItem("LotoSim/Reparar acciones de input")]
        public static void Ensure()
        {
            EnsureActions();
            EnsureSettings();
        }

        private static void EnsureActions()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            if (asset == null)
            {
                Debug.LogError($"[InputActionsGuard] No existe {ActionsPath}");
                return;
            }

            if (InputSystem.actions != asset)
                InputSystem.actions = asset;

            var preloaded = PlayerSettings.GetPreloadedAssets().Where(a => a != null).ToList();
            if (!preloaded.Contains(asset))
            {
                preloaded.Add(asset);
                PlayerSettings.SetPreloadedAssets(preloaded.ToArray());
                AssetDatabase.SaveAssets();
                Debug.Log("[InputActionsGuard] LotoSim.inputactions restaurado en Preloaded Assets.");
            }
        }

        private static void EnsureSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<InputSettings>(SettingsPath);
            bool created = false;
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<InputSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
                created = true;
            }

            bool changed = false;
            if (settings.editorInputBehaviorInPlayMode != InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView)
            {
                settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                changed = true;
            }
            if (changed || created)
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }

            if (InputSystem.settings != settings)
                InputSystem.settings = settings;
            if (created)
                Debug.Log("[InputActionsGuard] Creado LotoSim.inputsettings (el teclado llega siempre a la Game view en el Editor).");
        }
    }
}
