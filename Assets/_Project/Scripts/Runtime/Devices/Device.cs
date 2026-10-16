using LotoSim.Core;
using LotoSim.Interaction;
using UnityEngine;

namespace LotoSim.Devices
{
    /// <summary>
    /// Dispositivo identificable del equipo (seccionador, válvula…). Si tiene un LotoSocket,
    /// la misma interacción coloca o retira dispositivos LOTO según lo que el usuario tenga en mano.
    /// </summary>
    public abstract class Device : MonoBehaviour, IInteractable
    {
        [SerializeField, Tooltip("Identificador según la tarjeta de LOTO, ej. DS-101")]
        private string _deviceId;

        [SerializeField, Tooltip("Nombre con artículo, ej. \"la válvula de descarga V-102\"")]
        private string _displayName;

        private LotoSocket _socket;

        private enum ActionKind
        {
            Operate,
            Place,
            Remove,
            Blocked
        }

        public string DeviceId => _deviceId;
        public string DisplayName => _displayName;
        public LotoSocket Socket => _socket;
        public bool IsLocked => _socket != null && _socket.IsLocking;

        public abstract DeviceState State { get; }

        /// <summary>True si el dispositivo está en su posición segura (OFF / cerrado).</summary>
        public virtual bool IsIsolated => false;

        /// <summary>Qué hacer para dejarlo en posición segura.</summary>
        public virtual string IsolateHint => "Primero aislá el dispositivo";

        protected abstract string OperatePrompt { get; }

        protected virtual void Awake() => TryGetComponent(out _socket);

        protected virtual void OnEnable() => DeviceRegistry.Register(this);

        protected virtual void OnDisable() => DeviceRegistry.Unregister(this);

        public bool CanInteract(IInteractor interactor) => Resolve(interactor, out _) != ActionKind.Blocked;

        public string GetPrompt(IInteractor interactor)
        {
            Resolve(interactor, out var prompt);
            return prompt;
        }

        public void Interact(IInteractor interactor)
        {
            var selected = SelectedItem(interactor);
            switch (Resolve(interactor, out _))
            {
                case ActionKind.Place:
                    _socket.Place(interactor.Kit, selected);
                    break;
                case ActionKind.Remove:
                    _socket.RemoveTop(interactor.Kit);
                    break;
                case ActionKind.Operate:
                    Operate();
                    break;
            }
        }

        protected abstract void Operate();

        protected void PublishState() => SimEvents.Publish(new DeviceStateChanged(_deviceId, State));

        private ActionKind Resolve(IInteractor interactor, out string prompt)
        {
            var selected = SelectedItem(interactor);
            string placeReason = null;

            if (_socket != null && selected != LotoItemType.None)
            {
                if (_socket.CanPlace(selected, out placeReason))
                {
                    prompt = $"Colocar {LotoItems.WithArticle(selected)} en {_displayName}";
                    return ActionKind.Place;
                }
            }

            if (_socket != null && _socket.HasAny && selected == LotoItemType.None)
            {
                bool canRemove = _socket.CanRemoveTop(interactor.Kit, out var removeReason);
                prompt = canRemove ? $"Retirar {LotoItems.WithArticle(_socket.TopItem)} de {_displayName}" : removeReason;
                return canRemove ? ActionKind.Remove : ActionKind.Blocked;
            }

            if (!IsLocked)
            {
                prompt = OperatePrompt;
                return ActionKind.Operate;
            }

            prompt = placeReason ?? "Bloqueado con LOTO: no se puede operar";
            return ActionKind.Blocked;
        }

        private static LotoItemType SelectedItem(IInteractor interactor) =>
            interactor.Kit != null ? interactor.Kit.Selected : LotoItemType.None;
    }
}
