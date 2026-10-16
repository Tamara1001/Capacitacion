using UnityEngine;

namespace LotoSim.Interaction
{
    /// <summary>
    /// Menú superpuesto activo (mapa de estaciones, tablet) que captura las teclas numéricas.
    /// Sólo uno a la vez: el dueño lo abre y lo cierra.
    /// </summary>
    public static class OverlayMenu
    {
        public static object Owner { get; private set; }
        public static bool IsOpen => Owner != null;

        public static bool IsOpenBy(object owner) => owner != null && Owner == owner;

        public static bool TryOpen(object owner)
        {
            if (Owner != null && Owner != owner)
                return false;
            Owner = owner;
            return true;
        }

        public static void Close(object owner)
        {
            if (Owner == owner)
                Owner = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Owner = null;
    }
}
