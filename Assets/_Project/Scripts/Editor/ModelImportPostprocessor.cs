using UnityEditor;
using UnityEngine;

namespace LotoSim.Editor
{
    /// <summary>
    /// Configuración automática de los FBX de Assets/_Project/Art/Models: sin animación,
    /// UV2 para lightmaps y materiales remapeados por nombre a los M_GB_* existentes.
    /// </summary>
    public sealed class ModelImportPostprocessor : AssetPostprocessor
    {
        private const string ModelsFolder = "Assets/_Project/Art/Models/";
        private const string MaterialsFolder = "Assets/_Project/Art/Materials/Greybox/";

        private bool IsProjectModel => assetPath.StartsWith(ModelsFolder);

        private void OnPreprocessModel()
        {
            if (!IsProjectModel)
                return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.importVisibility = false;
            importer.generateSecondaryUV = true;
            importer.useFileScale = true;
            importer.globalScale = 1f;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.importNormals = ModelImporterNormals.Import;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.External;
            importer.materialName = ModelImporterMaterialName.BasedOnMaterialName;
            importer.materialSearch = ModelImporterMaterialSearch.Local;
        }

        private Material OnAssignMaterialModel(Material material, Renderer renderer)
        {
            if (!IsProjectModel)
                return null;
            var existing = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}{material.name}.mat");
            if (existing == null)
                Debug.LogWarning($"[Models] Material sin equivalente en Unity: {material.name} ({assetPath})");
            return existing;
        }
    }
}
