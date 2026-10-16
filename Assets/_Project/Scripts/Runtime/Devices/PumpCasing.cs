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

        private bool _inProgress;

        public bool CanInteract(IInteractor interactor) => !_inProgress && !Signals.HasFlag(MaintenanceDoneFlag);

        public string GetPrompt(IInteractor interactor) =>
            Signals.HasFlag(MaintenanceDoneFlag) ? "Sello mecánico reemplazado" : "Realizar el cambio de sello mecánico";

        public void Interact(IInteractor interactor)
        {
            _inProgress = true;
            Signals.Emit(MaintenanceStartedSignal);
            ScreenFade.Run(CompleteMaintenance);
        }

        private void CompleteMaintenance()
        {
            _inProgress = false;
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
