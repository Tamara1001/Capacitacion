using LotoSim.Interaction;
using UnityEngine;

namespace LotoSim.Devices
{
    /// <summary>Botón físico de la botonera (START o STOP).</summary>
    [DisallowMultipleComponent]
    public sealed class ControlButton : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private ControlStation _station;

        [SerializeField]
        private bool _isStart;

        public bool CanInteract(IInteractor interactor) => _station != null;

        public string GetPrompt(IInteractor interactor) => _isStart ? "Presionar START" : "Presionar STOP";

        public void Interact(IInteractor interactor)
        {
            if (_isStart)
                _station.PressStart();
            else
                _station.PressStop();
        }
    }
}
