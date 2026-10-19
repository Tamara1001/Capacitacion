using LotoSim.Core;
using UnityEngine;

namespace LotoSim.Interaction
{
    /// <summary>Estación de bloqueo en la pared: entrega y recibe el kit LOTO.</summary>
    [DisallowMultipleComponent]
    public sealed class LotoStation : MonoBehaviour, IInteractable
    {
        public const string KitCollectedFlag = "kit.collected";
        public const string KitReturnedFlag = "kit.returned";
        private const string MaintenanceDoneFlag = "maintenance.done";

        public bool CanInteract(IInteractor interactor)
        {
            var kit = interactor.Kit;
            if (kit == null || Signals.HasFlag(KitReturnedFlag))
                return false;
            if (!kit.IsCollected)
                return true;
            return kit.IsComplete;
        }

        public string GetPrompt(IInteractor interactor)
        {
            var kit = interactor.Kit;
            if (kit == null || Signals.HasFlag(KitReturnedFlag))
                return "Kit LOTO guardado";
            if (!kit.IsCollected)
                return "Tomar el kit LOTO";
            if (!kit.IsComplete)
                return "Retirá todos los dispositivos antes de devolver el kit";
            return Signals.HasFlag(MaintenanceDoneFlag) ? "Devolver el kit LOTO" : "Dejar el kit LOTO (todavía no lo usaste)";
        }

        public void Interact(IInteractor interactor)
        {
            var kit = interactor.Kit;
            if (!kit.IsCollected)
            {
                kit.Collect();
                Signals.RaiseFlag(KitCollectedFlag);
                Toasts.Show("Kit LOTO tomado. Seleccioná dispositivos con 1, 2 y 3.", ToastKind.Success);
                return;
            }
            kit.Stow();
            // Dejar el kit antes de terminar el trabajo no cierra el procedimiento: se puede volver a tomar.
            if (!Signals.HasFlag(MaintenanceDoneFlag))
            {
                Toasts.Show("Dejaste el kit en la estación. Podés volver a tomarlo cuando lo necesites.", ToastKind.Warning);
                return;
            }
            Signals.RaiseFlag(KitReturnedFlag);
            Toasts.Show("Kit LOTO devuelto a la estación.", ToastKind.Success);
        }
    }
}
