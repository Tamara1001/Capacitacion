using LotoSim.Core;
using LotoSim.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LotoSim.Player
{
    /// <summary>Movimiento en primera persona con WASD (desktop). En VR lo reemplaza la locomoción XR.</summary>
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

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _move = InputSystem.actions.FindAction("Player/Move", true);
            _sprint = InputSystem.actions.FindAction("Player/Sprint", true);
        }

        private void Update()
        {
            bool canMove = GameFlow.State == GameState.Playing && !DialogueSession.IsOpen && CursorControl.HasControl;
            var input = canMove ? _move.ReadValue<Vector2>() : Vector2.zero;
            float speed = _sprint.IsPressed() ? _sprintSpeed : _walkSpeed;

            var horizontal = (transform.right * input.x + transform.forward * input.y) * speed;
            _verticalVelocity = _controller.isGrounded ? -1f : _verticalVelocity + _gravity * Time.deltaTime;

            _controller.Move((horizontal + Vector3.up * _verticalVelocity) * Time.deltaTime);
        }

        /// <summary>Teletransporta respetando el CharacterController.</summary>
        public void Teleport(Vector3 position, float yaw)
        {
            _controller.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            _controller.enabled = true;
        }
    }
}
