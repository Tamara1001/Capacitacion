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
            return Signals.HasFlag(MaintenanceDoneFlag) && kit.IsComplete;
        }

        public string GetPrompt(IInteractor interactor)
        {
            var kit = interactor.Kit;
            if (kit == null || Signals.HasFlag(KitReturnedFlag))
                return "Kit LOTO guardado";
            if (!kit.IsCollected)
                return "Tomar el kit LOTO";
            if (!Signals.HasFlag(MaintenanceDoneFlag))
                return "Kit LOTO en uso";
            return kit.IsComplete
                ? "Devolver el kit LOTO"
                : "Retirá todos los dispositivos antes de devolver el kit";
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
            Signals.RaiseFlag(KitReturnedFlag);
            Toasts.Show("Kit LOTO devuelto a la estación.", ToastKind.Success);
        }
    }
}
