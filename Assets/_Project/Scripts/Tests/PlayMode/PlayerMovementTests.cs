using System.Collections;
using LotoSim.Core;
using LotoSim.Interaction;
using LotoSim.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LotoSim.Tests
{
    /// <summary>Verifica que WASD mueva al jugador (cableado de acciones + gating de cursor/estado).</summary>
    public class PlayerMovementTests
    {
        private InputSettings.BackgroundBehavior _previousBackground;

        // Los tests inyectan eventos de teclado: que el foco de la ventana no los descarte.
        [SetUp]
        public void SetUp()
        {
            _previousBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        }

        [TearDown]
        public void TearDown() => InputSystem.settings.backgroundBehavior = _previousBackground;

        /// <summary>Espera tiempo de juego (no frames): el editor puede correr lento sin foco.</summary>
        private static IEnumerator WaitGameSeconds(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end)
                yield return null;
        }

        [UnityTest]
        public IEnumerator PressingW_MovesPlayerForward()
        {
            yield return SceneManager.LoadSceneAsync("PumpRoom");
            yield return null;

            Assert.IsNotNull(InputSystem.actions, "InputSystem.actions es null: falta LotoSim.inputactions en Preloaded Assets");
            GameFlow.SetState(GameState.Playing);
            CursorControl.EnableFallback();

            var motor = Object.FindFirstObjectByType<FirstPersonMotor>();
            var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            InputSystem.EnableDevice(keyboard);
            yield return null;

            var start = motor.transform.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            yield return WaitGameSeconds(0.6f);
            var moved = Vector3.Distance(start, motor.transform.position);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;

            Assert.Greater(moved, 0.5f, $"W no movió al jugador (distancia {moved:F3}, estado {GameFlow.State}, control {CursorControl.HasControl})");
        }

        [UnityTest]
        public IEnumerator TextOnlyKeyboard_TypedW_MovesPlayerAndTypedTOpensStationMap()
        {
            yield return SceneManager.LoadSceneAsync("PumpRoom");
            yield return null;

            GameFlow.SetState(GameState.Playing);
            CursorControl.EnableFallback();
            var motor = Object.FindFirstObjectByType<FirstPersonMotor>();
            var stations = Object.FindFirstObjectByType<StationTeleport>();
            var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            InputSystem.EnableDevice(keyboard);
            yield return null;

            // Teclado virtual / streaming: llegan caracteres, ninguna tecla física pasa a "presionada".
            var start = motor.transform.position;
            float end = Time.time + 0.8f;
            while (Time.time < end)
            {
                InputSystem.QueueTextEvent(keyboard, 'w');
                yield return new WaitForSeconds(0.1f);
            }
            float moved = Vector3.Distance(start, motor.transform.position);
            Assert.IsFalse(keyboard.wKey.isPressed, "El escenario no debe tener la tecla física presionada");
            Assert.Greater(moved, 0.5f, $"Las 'w' tipeadas no movieron al jugador (distancia {moved:F3})");

            InputSystem.QueueTextEvent(keyboard, 't');
            yield return null;
            yield return null;
            Assert.IsTrue(stations.IsOpen, "La 't' tipeada debía abrir el mapa de estaciones");
        }

        [UnityTest]
        public IEnumerator MoveActionDisabled_ArrowKey_StillMovesViaKeyboardFallback()
        {
            yield return SceneManager.LoadSceneAsync("PumpRoom");
            yield return null;

            GameFlow.SetState(GameState.Playing);
            CursorControl.EnableFallback();
            var motor = Object.FindFirstObjectByType<FirstPersonMotor>();
            var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            InputSystem.EnableDevice(keyboard);
            InputSystem.actions.FindAction("Player/Move", true).Disable(); // simula acciones mal resueltas
            yield return null;

            var start = motor.transform.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.UpArrow));
            yield return WaitGameSeconds(0.6f);
            float moved = Vector3.Distance(start, motor.transform.position);
            bool usedFallback = motor.UsedKeyboardFallback;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.actions.FindAction("Player/Move", true).Enable();
            yield return null;

            Assert.Greater(moved, 0.5f, $"La flecha arriba no movió al jugador (distancia {moved:F3})");
            Assert.IsTrue(usedFallback, "El movimiento debía venir del respaldo de teclado directo");
        }
    }
}
