using System.Collections.Generic;
using System.IO;
using LotoSim.Audio;
using UnityEditor;
using UnityEngine;

namespace LotoSim.Editor
{
    /// <summary>Genera la biblioteca de sonidos a partir de los clips de Assets/_Project/Audio (por prefijo de nombre).</summary>
    public static class AudioDataBuilder
    {
        public const string LibraryPath = "Assets/_Project/Audio/Cues/SND_Library.asset";
        private const string AudioFolder = "Assets/_Project/Audio/";

        [MenuItem("LotoSim/Datos/Regenerar biblioteca de sonidos", priority = 22)]
        public static SoundLibrary Build()
        {
            var entries = new List<SoundEntry>
            {
                Entry(SoundId.SwitchLever, "SFX/SFX_SwitchLever", 0.9f, 0.04f),
                Entry(SoundId.ValveTurn, "SFX/SFX_ValveTurn", 0.7f, 0.08f),
                Entry(SoundId.PadlockClose, "SFX/SFX_PadlockClose", 0.8f, 0.06f),
                Entry(SoundId.PadlockOpen, "SFX/SFX_PadlockOpen", 0.8f, 0.04f),
                Entry(SoundId.TagAttach, "SFX/SFX_TagAttach", 0.6f, 0.1f),
                Entry(SoundId.LockoutAttach, "SFX/SFX_LockoutAttach", 0.8f, 0.05f),
                Entry(SoundId.Button, "SFX/SFX_Button", 0.7f, 0.03f),
                Entry(SoundId.PickUp, "SFX/SFX_PickUp", 0.5f, 0.1f),
                Entry(SoundId.Footstep, "SFX/SFX_Footstep", 0.25f, 0.08f),
                Entry(SoundId.Spark, "SFX/SFX_Spark", 0.9f, 0.05f),
                Entry(SoundId.UiClick, "UI/UI_Click", 0.5f, 0f),
                Entry(SoundId.UiConfirm, "UI/UI_Confirm", 0.35f, 0.02f),
                Entry(SoundId.UiStepComplete, "UI/UI_StepComplete", 0.55f, 0f),
                Entry(SoundId.UiError, "UI/UI_Error", 0.6f, 0f),
                Entry(SoundId.UiWarning, "UI/UI_Warning", 0.5f, 0f),
                Entry(SoundId.UiOpen, "UI/UI_Open", 0.45f, 0f),
                Entry(SoundId.UiClose, "UI/UI_Close", 0.4f, 0f),
                Entry(SoundId.UiTick, "UI/UI_Tick", 0.5f, 0.03f),
                Entry(SoundId.UiReport, "UI/UI_Report", 0.6f, 0f)
            };

            var library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<SoundLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }
            library.EditorInitialize(entries);
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            return library;
        }

        public static AudioClip Clip(string relativePathWithoutExtension)
        {
            foreach (var ext in new[] { ".wav", ".ogg" })
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}{relativePathWithoutExtension}{ext}");
                if (clip != null)
                    return clip;
            }
            Debug.LogWarning($"[Audio] Falta el clip {relativePathWithoutExtension}");
            return null;
        }

        /// <summary>Junta el clip exacto y sus variantes _0, _1, …</summary>
        private static SoundEntry Entry(SoundId id, string prefix, float volume, float pitchVariance)
        {
            var clips = new List<AudioClip>();
            var folder = Path.GetDirectoryName($"{AudioFolder}{prefix}")!.Replace('\\', '/');
            string name = Path.GetFileName(prefix);
            foreach (var guid in AssetDatabase.FindAssets($"t:AudioClip {name}", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                string file = Path.GetFileNameWithoutExtension(path);
                if (file == name || file.StartsWith(name + "_"))
                    clips.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(path));
            }
            if (clips.Count == 0)
                Debug.LogWarning($"[Audio] Sin clips para {id} ({prefix})");
            return new SoundEntry(id, clips.ToArray(), volume, pitchVariance);
        }
    }
}
