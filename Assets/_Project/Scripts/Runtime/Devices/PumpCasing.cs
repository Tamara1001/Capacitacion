using LotoSim.Core;
using LotoSim.Interaction;
using UnityEngine;

namespace LotoSim.Devices
{
    /// <summary>
    /// Carcasa de la bomba: dispara el mantenimiento (cambio de sello). En v1 no se juega el
    /// mantenimiento; al terminar aparecen herramientas olvidadas para el paso de cierre.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PumpCasing : MonoBehaviour, IInteractable
    {
        public const string MaintenanceDoneFlag = "maintenance.done";
        public const string MaintenanceStartedSignal = "maintenance.start";

        [SerializeField, Tooltip("Objetos que aparecen al terminar el mantenimiento")]
        private GameObject[] _spawnOnDone;

        public bool CanInteract(IInteractor interactor) => !Signals.HasFlag(MaintenanceDoneFlag);

        public string GetPrompt(IInteractor interactor) =>
            Signals.HasFlag(MaintenanceDoneFlag) ? "Sello mecánico reemplazado" : "Realizar el cambio de sello mecánico";

        public void Interact(IInteractor interactor)
        {
            Signals.Emit(MaintenanceStartedSignal);
            Signals.RaiseFlag(MaintenanceDoneFlag);
            foreach (var spawned in _spawnOnDone)
            {
                if (spawned != null)
                    spawned.SetActive(true);
            }
            Toasts.Show("Se realizó el cambio de sello mecánico.", ToastKind.Success);
        }
    }
}
