using System.Text;
using LotoSim.Core;
using LotoSim.Interaction;
using LotoSim.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace LotoSim.UI
{
    /// <summary>
    /// Diagnóstico de entrada (F3): muestra qué ve realmente el juego del teclado y del mouse.
    /// Sirve para entender por qué algo no responde en un equipo concreto (editor o build).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InputDebugOverlay : MonoBehaviour
    {
        [SerializeField]
        private GameObject _panel;

        [SerializeField]
        private TMP_Text _text;

        [SerializeField]
        private FirstPersonMotor _motor;

        private readonly StringBuilder _builder = new();
        private InputAction _move;
        private InputAction _look;
        private float _nextRefresh;

        private void Awake()
        {
            _move = InputSystem.actions?.FindAction("Player/Move");
            _look = InputSystem.actions?.FindAction("Player/Look");
            _panel.SetActive(false);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f3Key.wasPressedThisFrame)
                _panel.SetActive(!_panel.activeSelf);
            if (!_panel.activeSelf || Time.unscaledTime < _nextRefresh)
                return;
            _nextRefresh = Time.unscaledTime + 0.1f;
            _text.text = Describe(keyboard);
        }

        private string Describe(Keyboard keyboard)
        {
            _builder.Clear();
            _builder.Append("<b>DIAGNÓSTICO DE ENTRADA</b>  (F3 para ocultar)\n");
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
            _builder.Append($"Enfoque de la aplicación: {Application.isFocused}   Dispositivos: {InputSystem.devices.Count}");
            return _builder.ToString();
        }

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
