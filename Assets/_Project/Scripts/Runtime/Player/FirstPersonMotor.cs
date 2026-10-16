using LotoSim.Core;
using LotoSim.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LotoSim.Player
{
    /// <summary>
    /// Movimiento en primera persona (WASD o flechas). En VR lo reemplaza la locomoción XR.
    /// Lee la acción <c>Player/Move</c>; si llega en cero consulta el teclado directamente, de modo que
    /// el movimiento no dependa de que los bindings o el foco de las acciones estén bien resueltos.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public sealed class FirstPersonMotor : MonoBehaviour
    {
        [SerializeField]
        private float _walkSpeed = 2.2f;

        [SerializeField]
        private float _sprintSpeed = 3.5f;

        [SerializeField]
        private float _gravity = -9.81f;

        private CharacterController _controller;
        private InputAction _move;
        private InputAction _sprint;
        private float _verticalVelocity;

        /// <summary>Último vector de entrada aplicado (para diagnóstico).</summary>
        public Vector2 LastInput { get; private set; }

        /// <summary>True si el último movimiento vino del teclado directo y no de la acción.</summary>
        public bool UsedKeyboardFallback { get; private set; }

        public bool CanMove =>
            GameFlow.State == GameState.Playing && !DialogueSession.IsOpen && CursorControl.HasControl;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _move = InputSystem.actions.FindAction("Player/Move", true);
            _sprint = InputSystem.actions.FindAction("Player/Sprint", true);
        }

        private void Update()
        {
            var input = CanMove ? ReadMove() : Vector2.zero;
            LastInput = input;

            bool sprint = _sprint.IsPressed() || (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed);
            float speed = sprint ? _sprintSpeed : _walkSpeed;

            var horizontal = (transform.right * input.x + transform.forward * input.y) * speed;
            _verticalVelocity = _controller.isGrounded ? -1f : _verticalVelocity + _gravity * Time.deltaTime;

            _controller.Move((horizontal + Vector3.up * _verticalVelocity) * Time.deltaTime);
        }

        private Vector2 ReadMove()
        {
            var fromAction = _move.ReadValue<Vector2>();
            UsedKeyboardFallback = false;
            if (fromAction.sqrMagnitude > 0.0001f)
                return fromAction;

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return Vector2.zero;

            var raw = new Vector2(
                Axis(keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed, keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed),
                Axis(keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed, keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed));
            UsedKeyboardFallback = raw.sqrMagnitude > 0f;
            return raw.sqrMagnitude > 1f ? raw.normalized : raw;
        }

        private static float Axis(bool positive, bool negative) => (positive ? 1f : 0f) - (negative ? 1f : 0f);

        /// <summary>Teletransporta respetando el CharacterController.</summary>
        public void Teleport(Vector3 position, float yaw)
        {
            _controller.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            _controller.enabled = true;
        }
    }
}
