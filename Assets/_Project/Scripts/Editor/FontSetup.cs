using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace LotoSim.Editor
{
    /// <summary>Crea los font assets de TMP de Barlow y los define como fuente por defecto del proyecto.</summary>
    public static class FontSetup
    {
        private const string SourceFolder = "Assets/ThirdParty/Fonts/Barlow/";
        private const string OutputFolder = "Assets/_Project/UI/Fonts/";
        private const string Characters =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "¡¿ÁÉÍÓÚÜÑáéíóúüñ°±·×—–…“”‘’•→←↑↓✓";

        [MenuItem("LotoSim/UI/Crear fuentes Barlow", priority = 60)]
        public static void Build()
        {
            var regular = Create("Barlow-Regular");
            var semiBold = Create("Barlow-SemiBold");
            var bold = Create("Barlow-Bold");

            // <b> usa la variante Bold real; SemiBold queda como peso 600.
            regular.fontWeightTable[7].regularTypeface = bold;
            regular.fontWeightTable[6].regularTypeface = semiBold;
            EditorUtility.SetDirty(regular);

            var settings = TMP_Settings.instance;
            var so = new SerializedObject(settings);
            so.FindProperty("m_defaultFontAsset").objectReferenceValue = regular;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log("[LotoSim] Fuente por defecto: Barlow");
        }

        private static TMP_FontAsset Create(string name)
        {
            string path = $"{OutputFolder}{name} SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null)
                return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>($"{SourceFolder}{name}.ttf");
            var asset = TMP_FontAsset.CreateFontAsset(font, 72, 8, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            asset.name = $"{name} SDF";
            AssetDatabase.CreateAsset(asset, path);
            asset.atlasTextures[0].name = $"{name} Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            asset.material.name = $"{name} Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            asset.TryAddCharacters(Characters, out var missing);
            if (!string.IsNullOrEmpty(missing))
                Debug.Log($"[LotoSim] {name}: caracteres sin glifo: {missing}");
            EditorUtility.SetDirty(asset);
            return asset;
        }
    }
}
