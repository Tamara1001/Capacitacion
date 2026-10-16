using LotoSim.Core;
using UnityEngine;

namespace LotoSim.Interaction
{
    /// <summary>
    /// Interacción simple configurable: levanta un flag y/o emite una señal y muestra un aviso.
    /// Se usa para EPP, rótulos de identificación, herramientas, etc.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SignalInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField, Tooltip("Texto de la acción, ej. \"Colocarse el casco\"")]
        private string _prompt = "Interactuar";

        [SerializeField, Tooltip("Flag persistente que se levanta al usar (opcional)")]
        private string _flag;

        [SerializeField, Tooltip("Señal momentánea que se emite al usar (opcional)")]
        private string _signal;

        [SerializeField, Tooltip("Mensaje que se muestra al usar (opcional)")]
        private string _toast;

        [SerializeField]
        private ToastKind _toastKind = ToastKind.Success;

        [SerializeField, Tooltip("Desactiva el objeto al usarlo (ej. recoger algo)")]
        private bool _deactivateOnUse;

        public bool CanInteract(IInteractor interactor) => true;

        public string GetPrompt(IInteractor interactor) => _prompt;

        public void Interact(IInteractor interactor)
        {
            Signals.RaiseFlag(_flag);
            Signals.Emit(_signal);
            Toasts.Show(_toast, _toastKind);
            if (_deactivateOnUse)
                gameObject.SetActive(false);
        }
    }
}
