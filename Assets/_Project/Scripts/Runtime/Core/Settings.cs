using UnityEngine;

namespace LotoSim.Core
{
    /// <summary>Preferencias del usuario persistidas localmente (por navegador en WebGL).</summary>
    public static class Settings
    {
        private const string SensitivityKey = "loto.look.sensitivity";
        private const string InvertYKey = "loto.look.invertY";

        public const float DefaultSensitivity = 0.12f;

        public static float LookSensitivity
        {
            get => PlayerPrefs.GetFloat(SensitivityKey, DefaultSensitivity);
            set
            {
                PlayerPrefs.SetFloat(SensitivityKey, Mathf.Clamp(value, 0.02f, 0.5f));
                PlayerPrefs.Save();
            }
        }

        private const string LowQualityKey = "loto.gfx.low";

        /// <summary>Calidad gráfica baja (sin sombras ni post-procesado). Alta por defecto.</summary>
        public static bool LowQuality
        {
            get => PlayerPrefs.GetInt(LowQualityKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(LowQualityKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        private const string InputDiagKey = "loto.debug.inputDiag";

        /// <summary>Línea de diagnóstico de entrada en pantalla. Apagada por defecto; se activa desde Pausa o con F3.</summary>
        public static bool ShowInputDiagnostics
        {
            get => PlayerPrefs.GetInt(InputDiagKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(InputDiagKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        private const string VolumeKey = "loto.audio.master";

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 0.8f);
            set
            {
                PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
                AudioListener.volume = Mathf.Clamp01(value);
            }
        }

        public static bool InvertY
        {
            get => PlayerPrefs.GetInt(InvertYKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(InvertYKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}
