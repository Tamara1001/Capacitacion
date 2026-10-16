using LotoSim.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LotoSim.Interaction
{
    /// <summary>
    /// Interactor de escritorio: raycast desde el centro de la cámara + click.
    /// Es la única pieza de Interaction que lee input; en VR se reemplaza por un adaptador XR.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DesktopInteractor : MonoBehaviour, IInteractor
    {
        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private LotoKit _kit;

        [SerializeField, Tooltip("Distancia máxima para interactuar (m)")]
        private float _reach = 2f;

        [SerializeField, Tooltip("Distancia a la que se detecta un objeto y se pide acercarse (m)")]
        private float _detectRange = 6f;

        private InputAction _interact;
        private InputAction _stow;
        private InputAction _cycle;
        private InputAction[] _slots;
        private bool _wasLockedLastFrame;

        public Transform ViewOrigin => _camera.transform;
        public LotoKit Kit => _kit;

        /// <summary>Objeto apuntado (puede estar fuera de alcance).</summary>
        public IInteractable Target { get; private set; }
        public bool TargetInReach { get; private set; }
        public bool TargetEnabled { get; private set; }
        public string Prompt { get; private set; }

        private void Awake()
        {
            var actions = InputSystem.actions;
            _interact = actions.FindAction("Player/Interact", true);
            _stow = actions.FindAction("Player/Stow", true);
            _cycle = actions.FindAction("Player/CycleSlot", true);
            _slots = new[]
            {
                actions.FindAction("Player/Slot1", true),
                actions.FindAction("Player/Slot2", true),
                actions.FindAction("Player/Slot3", true)
            };
        }

        private void OnEnable() => InputSystem.actions.FindActionMap("Player", true).Enable();

        private void Update()
        {
            // El click que captura el cursor no debe interactuar a la vez.
            bool hasControl = CursorControl.HasControl;
            bool canAct = hasControl && _wasLockedLastFrame;
            _wasLockedLastFrame = hasControl;

            if (GameFlow.State != GameState.Playing)
            {
                ClearTarget();
                return;
            }
            if (DialogueSession.IsOpen)
            {
                ClearTarget();
                HandleDialogueInput();
                return;
            }
            if (OverlayMenu.IsOpen)
            {
                ClearTarget();
                return;
            }

            HandleKitInput();
            UpdateTarget();

            if (canAct && _interact.WasPressedThisFrame() && Target != null && TargetInReach && TargetEnabled)
                Target.Interact(this);
        }

        private void HandleDialogueInput()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].WasPressedThisFrame() || TextKeyBridge.Typed((char)('1' + i)))
                    DialogueSession.Choose(i);
            }
        }

        private void HandleKitInput()
        {
            if (_kit == null)
                return;
            if (_slots[0].WasPressedThisFrame() || TextKeyBridge.Typed('1')) _kit.Select(LotoItemType.Padlock);
            if (_slots[1].WasPressedThisFrame() || TextKeyBridge.Typed('2')) _kit.Select(LotoItemType.Tag);
            if (_slots[2].WasPressedThisFrame() || TextKeyBridge.Typed('3')) _kit.Select(LotoItemType.ValveLockout);
            if (_stow.WasPressedThisFrame() || TextKeyBridge.Typed('r')) _kit.ClearSelection();

            float scroll = _cycle.ReadValue<float>();
            if (Mathf.Abs(scroll) > 0.01f && _cycle.WasPressedThisFrame())
                _kit.Cycle(scroll > 0f ? -1 : 1);
        }

        private void UpdateTarget()
        {
            var ray = new Ray(_camera.transform.position, _camera.transform.forward);
            if (!Physics.Raycast(ray, out var hit, _detectRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                ClearTarget();
                return;
            }

            Target = hit.collider.GetComponentInParent<IInteractable>();
            if (Target == null)
            {
                ClearTarget();
                return;
            }

            TargetInReach = hit.distance <= _reach;
            TargetEnabled = Target.CanInteract(this);
            Prompt = TargetInReach ? Target.GetPrompt(this) : "Acercate";
        }

        private void ClearTarget()
        {
            Target = null;
            TargetInReach = false;
            TargetEnabled = false;
            Prompt = null;
        }
    }
}
