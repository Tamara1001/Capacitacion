using UnityEngine;

namespace LotoSim.Interaction
{
    /// <summary>
    /// Estado del cursor en escritorio. Si el navegador no permite capturar el puntero
    /// (iframes restringidos, vistas embebidas), se usa un modo de respaldo sin captura.
    /// </summary>
    public static class CursorControl
    {
        public static bool PointerLockUnavailable { get; private set; }

        public static bool IsLocked => Cursor.lockState == CursorLockMode.Locked;

        /// <summary>Si el jugador tiene el control: cursor capturado o modo de respaldo.</summary>
        public static bool HasControl => IsLocked || PointerLockUnavailable;

        public static void Lock()
        {
            if (PointerLockUnavailable)
                return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public static void Unlock()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public static void EnableFallback()
        {
            PointerLockUnavailable = true;
            Unlock();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => PointerLockUnavailable = false;
    }
}
