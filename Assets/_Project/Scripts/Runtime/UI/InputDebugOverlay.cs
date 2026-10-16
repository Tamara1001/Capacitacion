using System.Text;
using LotoSim.Core;
using LotoSim.Interaction;
using LotoSim.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace LotoSim.UI
{
    /// <summary>
    /// Diagnóstico de entrada para entender por qué algo no responde en un equipo concreto.
    /// - Línea compacta siempre visible (se apaga desde Pausa o con F3).
    /// - Panel completo con F3, o automáticamente si se aprietan teclas de movimiento y el jugador no avanza.
    /// No necesita teclas especiales: se puede ver y apagar con el mouse desde el menú de pausa.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InputDebugOverlay : MonoBehaviour
    {
        private const float StuckSeconds = 0.8f;
        private const float StuckDistance = 0.05f;

        [SerializeField]
        private GameObject _compactPanel;

        [SerializeField]
        private TMP_Text _compactText;

        [SerializeField]
        private GameObject _fullPanel;

        [SerializeField]
        private TMP_Text _fullText;

        [SerializeField]
        private FirstPersonMotor _motor;

        [SerializeField, Tooltip("Interruptor en la pantalla de pausa")]
        private Toggle _pauseToggle;

        private readonly StringBuilder _builder = new();
        private InputAction _move;
        private InputAction _look;
        private float _nextRefresh;
        private float _heldSeconds;
        private Vector3 _heldStart;
        private bool _autoExpanded;
        private bool _fullRequested;

        // Contadores de eventos crudos: dicen si ALGO del teclado/mouse llega al juego, aunque no sea WASD.
        private int _keyboardEvents;
        private int _mouseEvents;
        private int _textEvents;
        private char _lastChar;
        private float _lastKeyboardTime = -1f;
        private float _lastTextTime = -1f;
        private bool _loggedFirstKeyboard;
        private bool _loggedFirstText;
        private Keyboard _subscribedKeyboard;

        private bool Enabled => Settings.ShowInputDiagnostics;

        private void Awake()
        {
            _move = InputSystem.actions?.FindAction("Player/Move");
            _look = InputSystem.actions?.FindAction("Player/Look");
            _pauseToggle.SetIsOnWithoutNotify(Enabled);
            _pauseToggle.onValueChanged.AddListener(OnToggle);
            Apply();
        }

        private void OnEnable()
        {
            InputSystem.onEvent += OnInputEvent;
            InputSystem.onDeviceChange += OnDeviceChange;
            SubscribeText();
        }

        private void OnDisable()
        {
            InputSystem.onEvent -= OnInputEvent;
            InputSystem.onDeviceChange -= OnDeviceChange;
            UnsubscribeText();
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device is Keyboard)
                SubscribeText();
        }

        private void SubscribeText()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || keyboard == _subscribedKeyboard)
                return;
            UnsubscribeText();
            _subscribedKeyboard = keyboard;
            keyboard.onTextInput += OnTextInput;
        }

        private void UnsubscribeText()
        {
            if (_subscribedKeyboard != null)
                _subscribedKeyboard.onTextInput -= OnTextInput;
            _subscribedKeyboard = null;
        }

        private void OnTextInput(char c)
        {
            _textEvents++;
            _lastChar = c;
            _lastTextTime = Time.unscaledTime;
            if (!_loggedFirstText)
            {
                _loggedFirstText = true;
                Debug.Log($"[Entrada] Llegó texto del teclado: '{c}'. Si las teclas WASD no se ponen en verde pero esto sí llega, el teclado manda caracteres y no teclas físicas (típico de teclados virtuales o de streaming).");
            }
        }

        private void OnInputEvent(UnityEngine.InputSystem.LowLevel.InputEventPtr eventPtr, InputDevice device)
        {
            if (device is Keyboard)
            {
                _keyboardEvents++;
                _lastKeyboardTime = Time.unscaledTime;
                if (!_loggedFirstKeyboard)
                {
                    _loggedFirstKeyboard = true;
                    Debug.Log($"[Entrada] Llegó el primer evento de teclado ({device.displayName}, habilitado: {device.enabled}).");
                }
            }
            else if (device is Mouse)
            {
                _mouseEvents++;
            }
        }

        private void OnToggle(bool value)
        {
            Settings.ShowInputDiagnostics = value;
            _fullRequested = false;
            _autoExpanded = false;
            Apply();
        }

        private void Apply()
        {
            bool playing = GameFlow.State == GameState.Playing || GameFlow.State == GameState.Paused;
            _compactPanel.SetActive(Enabled && playing && !ShowFull);
            _fullPanel.SetActive(Enabled && playing && ShowFull);
        }

        private bool ShowFull => _fullRequested || _autoExpanded;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f3Key.wasPressedThisFrame)
            {
                if (!Enabled)
                {
                    Settings.ShowInputDiagnostics = true;
                    _pauseToggle.SetIsOnWithoutNotify(true);
                    _fullRequested = true;
                }
                else
                {
                    _fullRequested = !ShowFull;
                    _autoExpanded = false;
                }
            }

            TrackStuck(keyboard);
            Apply();
            if (!Enabled || Time.unscaledTime < _nextRefresh)
                return;
            _nextRefresh = Time.unscaledTime + 0.1f;
            if (ShowFull)
                _fullText.text = DescribeFull(keyboard);
            else
                _compactText.text = DescribeCompact(keyboard);
        }

        /// <summary>Si se aprietan teclas de movimiento ~0,8 s y no hubo desplazamiento, abre el panel completo.</summary>
        private void TrackStuck(Keyboard keyboard)
        {
            bool playing = GameFlow.State == GameState.Playing && _motor != null;
            if (!playing || keyboard == null || !AnyMoveKey(keyboard))
            {
                _heldSeconds = 0f;
                return;
            }
            if (_heldSeconds == 0f)
                _heldStart = _motor.transform.position;
            _heldSeconds += Time.unscaledDeltaTime;
            if (_heldSeconds >= StuckSeconds && !_autoExpanded &&
                (_motor.transform.position - _heldStart).sqrMagnitude < StuckDistance * StuckDistance)
            {
                _autoExpanded = true;
            }
        }

        private static bool AnyMoveKey(Keyboard k) =>
            k.wKey.isPressed || k.aKey.isPressed || k.sKey.isPressed || k.dKey.isPressed ||
            k.upArrowKey.isPressed || k.downArrowKey.isPressed || k.leftArrowKey.isPressed || k.rightArrowKey.isPressed;

        private string DescribeCompact(Keyboard keyboard)
        {
            _builder.Clear();
            _builder.Append("<color=#9CA3AF>ENTRADA</color>  ");
            _builder.Append(CursorControl.HasControl ? "<color=#3BB273>control sí</color>" : "<color=#E5484D>sin control (hacé click)</color>");
            _builder.Append("  ·  ");
            if (keyboard == null)
                _builder.Append("<color=#E5484D>sin teclado</color>");
            else if (!keyboard.enabled)
                _builder.Append("<color=#E5484D>teclado deshabilitado</color>");
            else
                _builder.Append($"{Mark("W", keyboard.wKey)} {Mark("A", keyboard.aKey)} {Mark("S", keyboard.sKey)} {Mark("D", keyboard.dKey)}");
            _builder.Append($"  ·  Move {(_move != null ? _move.ReadValue<Vector2>().ToString("F1") : "?")}");
            if (_motor != null)
                _builder.Append($"  ·  motor {_motor.LastInput.ToString("F1")}");
            _builder.Append($"  ·  eventos: teclado {_keyboardEvents}  mouse {_mouseEvents}  texto {_textEvents}");
            if (_textEvents > 0)
                _builder.Append($" ('{_lastChar}')");
            _builder.Append("  <color=#6B7280>(F3: detalle)</color>");
            return _builder.ToString();
        }

        private string DescribeFull(Keyboard keyboard)
        {
            _builder.Clear();
            _builder.Append("<b>DIAGNÓSTICO DE ENTRADA</b>  (F3 o interruptor en Pausa para cerrar)\n");
            _builder.Append($"Estado: {GameFlow.State}   Diálogo: {DialogueSession.IsOpen}   Menú: {OverlayMenu.IsOpen}\n");
            _builder.Append($"Cursor: {Cursor.lockState}   Respaldo sin lock: {CursorControl.PointerLockUnavailable}   Control: {CursorControl.HasControl}\n");
            _builder.Append($"Acciones: {(InputSystem.actions != null ? InputSystem.actions.name : "NULL")}   Move habilitada: {_move != null && _move.enabled}\n");
            _builder.Append($"Move = {(_move != null ? _move.ReadValue<Vector2>().ToString("F2") : "?")}   Look = {(_look != null ? _look.ReadValue<Vector2>().ToString("F1") : "?")}\n");

            if (keyboard == null)
            {
                _builder.Append("<color=#E5484D>No hay teclado detectado</color>\n");
            }
            else
            {
                _builder.Append($"Teclado: {keyboard.displayName}  habilitado: {keyboard.enabled}  (verde = presionada)\n");
                _builder.Append($"{Mark("W", keyboard.wKey)}  {Mark("A", keyboard.aKey)}  {Mark("S", keyboard.sKey)}  {Mark("D", keyboard.dKey)}   {Mark("Shift", keyboard.leftShiftKey)}\n");
                _builder.Append($"Teclas presionadas: {PressedKeys(keyboard)}\n");
            }

            if (_motor != null)
            {
                _builder.Append($"Motor: puede moverse = {_motor.CanMove}   entrada aplicada = {_motor.LastInput.ToString("F2")}");
                _builder.Append(_motor.UsedKeyboardFallback ? "   (vía teclado directo)\n" : "\n");
                _builder.Append($"Posición: {_motor.transform.position.ToString("F2")}\n");
            }
            _builder.Append($"Eventos recibidos: teclado {_keyboardEvents} (último hace {Ago(_lastKeyboardTime)})   mouse {_mouseEvents}   texto {_textEvents} (último '{(_textEvents > 0 ? _lastChar.ToString() : "-")}' hace {Ago(_lastTextTime)})\n");
            _builder.Append($"Foco de la aplicación: {Application.isFocused}   Dispositivos: {InputSystem.devices.Count}   Plataforma: {Application.platform}\n");
            _builder.Append($"<color=#FFC629>Diagnóstico: {Verdict(keyboard)}</color>");
            return _builder.ToString();
        }

        /// <summary>Primera causa probable, en lenguaje llano.</summary>
        private string Verdict(Keyboard keyboard)
        {
            if (GameFlow.State != GameState.Playing)
                return $"el juego está en estado {GameFlow.State}, no se puede mover.";
            if (DialogueSession.IsOpen)
                return "hay un diálogo abierto (Esc para cerrarlo).";
            if (!CursorControl.HasControl)
                return "el mouse no está capturado: hacé click sobre la pantalla del juego.";
            if (keyboard == null)
                return "el juego no detecta ningún teclado.";
            if (!keyboard.enabled)
                return "el teclado está deshabilitado por el sistema (probablemente falta el foco de la ventana).";
            if (!AnyMoveKey(keyboard))
            {
                if (_keyboardEvents == 0 && _textEvents == 0)
                    return "no llega NINGÚN evento de teclado al juego (ni teclas ni texto): el teclado no está llegando a esta ventana.";
                if (_textEvents > 0 && _keyboardEvents == 0)
                    return "llega texto pero no teclas físicas: el teclado envía caracteres (teclado virtual / streaming), no se puede mantener W.";
                return "llegan eventos de teclado, pero no WASD/flechas: probá apretar W mantenida y mirá si se pone en verde.";
            }
            if (_motor != null && _motor.LastInput.sqrMagnitude < 0.0001f)
                return "las teclas llegan pero el motor no recibe entrada.";
            return "la entrada llega al motor; si no avanzás, algo físico te bloquea (colisión).";
        }

        private static string Ago(float time) =>
            time < 0f ? "nunca" : $"{Time.unscaledTime - time:F1} s";

        private static string Mark(string label, KeyControl key) =>
            key.isPressed ? $"<b><color=#3BB273>{label}</color></b>" : $"<color=#6B7280>{label}</color>";

        private static string PressedKeys(Keyboard keyboard)
        {
            var pressed = new StringBuilder();
            foreach (var key in keyboard.allKeys)
            {
                if (key.isPressed)
                    pressed.Append(key.displayName).Append(' ');
            }
            return pressed.Length == 0 ? "—" : pressed.ToString();
        }
    }
}
