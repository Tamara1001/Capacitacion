using UnityEditor;

namespace LotoSim.Editor
{
    /// <summary>Importación de audio del proyecto: mono, Vorbis y carga según el tipo de sonido.</summary>
    public sealed class AudioImportPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/_Project/Audio/"))
                return;
            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = true;
            importer.loadInBackground = assetPath.Contains("/Ambience/");
            var settings = importer.defaultSampleSettings;
            settings.compressionFormat = UnityEngine.AudioCompressionFormat.Vorbis;
            settings.quality = 0.55f;
            settings.loadType = assetPath.Contains("/Ambience/")
                ? UnityEngine.AudioClipLoadType.CompressedInMemory
                : UnityEngine.AudioClipLoadType.DecompressOnLoad;
            importer.defaultSampleSettings = settings;
        }
    }
}
