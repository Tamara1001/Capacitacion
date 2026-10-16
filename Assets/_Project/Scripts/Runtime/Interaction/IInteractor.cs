using UnityEngine;

namespace LotoSim.Interaction
{
    /// <summary>
    /// Quien interactúa con el mundo. Hoy: mouse + cámara (DesktopInteractor).
    /// En VR: una mano/controlador. La lógica del mundo sólo conoce esta interfaz.
    /// </summary>
    public interface IInteractor
    {
        Transform ViewOrigin { get; }
        LotoKit Kit { get; }
    }
}
