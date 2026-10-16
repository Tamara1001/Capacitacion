using UnityEngine;

namespace LotoSim.Interaction
{
    /// <summary>Indica que hay un menú superpuesto (ej. mapa de estaciones) que captura las teclas numéricas.</summary>
    public static class OverlayMenu
    {
        public static bool IsOpen { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => IsOpen = false;
    }
}
