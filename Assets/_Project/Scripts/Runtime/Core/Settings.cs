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
