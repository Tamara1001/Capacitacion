using LotoSim.Core;
using LotoSim.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LotoSim.Player
{
    /// <summary>
    /// Mirada con mouse y manejo del cursor. En WebGL el navegador libera el pointer lock con Esc:
    /// si se pierde mientras se juega, se pausa. Si el navegador no permite el lock, pasa a un
    /// modo de respaldo donde la cámara sigue al mouse sin capturarlo.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DesktopLook : MonoBehaviour
    {
        [SerializeField, Tooltip("Transform que gira en yaw (el cuerpo del jugador)")]
        private Transform _body;

        [SerializeField, Tooltip("Transform que gira en pitch (la cámara)")]
        private Transform _pitchPivot;

        [SerializeField, Tooltip("Grados por pixel de movimiento del mouse")]
        private float _sensitivity = 0.12f;

        [SerializeField]
        private float _maxPitch = 80f;

        [SerializeField, Tooltip("Frames que se espera el pointer lock antes de pasar al modo de respaldo")]
        private int _lockTimeoutFrames = 20;

        private InputAction _look;
        private InputAction _interact;
        private InputAction _pause;
        private float _pitch;
        private bool _wasLocked;
        private int _lockRequestFrame = -1;
        private bool _invertY;

        /// <summary>Relee sensibilidad e inversión desde las preferencias guardadas.</summary>
        public void ReloadSettings()
        {
            _sensitivity = Settings.LookSensitivity;
            _invertY = Settings.InvertY;
        }

        private void Awake()
        {
            ReloadSettings();
            _look = InputSystem.actions.FindAction("Player/Look", true);
            _interact = InputSystem.actions.FindAction("Player/Interact", true);
            _pause = InputSystem.actions.FindAction("Player/Pause", true);
        }

        private void OnEnable() => SimEvents.Subscribe<GameStateChanged>(OnGameStateChanged);

        private void OnDisable() => SimEvents.Unsubscribe<GameStateChanged>(OnGameStateChanged);

        private void Update()
        {
            bool locked = CursorControl.IsLocked;

            if (_pause.WasPressedThisFrame())
            {
                if (DialogueSession.IsOpen)
                    DialogueSession.Close();
                else
                    GameFlow.TogglePause();
            }
            else if (GameFlow.State == GameState.Playing && !CursorControl.PointerLockUnavailable)
            {
                UpdatePointerLock(locked);
            }

            if (GameFlow.State == GameState.Playing && CursorControl.HasControl && !DialogueSession.IsOpen)
                ApplyLook(_look.ReadValue<Vector2>());

            _wasLocked = CursorControl.IsLocked;
        }

        private void UpdatePointerLock(bool locked)
        {
            if (locked)
            {
                _lockRequestFrame = -1;
                return;
            }
            if (_wasLocked && Application.isFocused)
            {
                GameFlow.SetState(GameState.Paused);
            }
            else if (_interact.WasPressedThisFrame())
            {
                CursorControl.Lock();
                _lockRequestFrame = Time.frameCount;
            }
            else if (_lockRequestFrame >= 0 && Time.frameCount - _lockRequestFrame > _lockTimeoutFrames)
            {
                _lockRequestFrame = -1;
                CursorControl.EnableFallback();
                Toasts.Show("Tu navegador no permite capturar el mouse: la cámara sigue al puntero.", ToastKind.Warning);
            }
        }

        private void ApplyLook(Vector2 delta)
        {
            _body.Rotate(Vector3.up, delta.x * _sensitivity, Space.World);
            float vertical = _invertY ? -delta.y : delta.y;
            _pitch = Mathf.Clamp(_pitch - vertical * _sensitivity, -_maxPitch, _maxPitch);
            _pitchPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void OnGameStateChanged(GameStateChanged evt)
        {
            if (evt.State != GameState.Playing)
                CursorControl.Unlock();
            _wasLocked = false;
        }
    }
}
