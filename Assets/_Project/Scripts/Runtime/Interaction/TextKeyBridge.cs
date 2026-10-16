using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace LotoSim.Interaction
{
    /// <summary>
    /// Puente para teclados que sólo envían <b>caracteres</b> y no teclas físicas (teclados virtuales, streaming
    /// desde el celular, escritorio remoto). En ese caso el Input System recibe eventos de texto ('w') pero
    /// ninguna tecla pasa a "presionada", así que las acciones nunca se disparan.
    /// Este puente convierte los caracteres en:
    ///   · pulsos de movimiento (w/a/s/d), de <see cref="PulseSeconds"/> cada uno, y
    ///   · "teclas tipeadas este frame" para los atajos (<see cref="Typed"/>).
    /// Si el teclado es físico se ignora el texto (las acciones ya hacen su trabajo).
    /// </summary>
    public static class TextKeyBridge
    {
        public const float PulseSeconds = 0.4f;
        private const float PhysicalGraceSeconds = 0.15f;
        private const int BufferSize = 16;

        private static readonly char[] Buffer = new char[BufferSize];
        private static int _count;
        private static int _frame = -1;
        private static Vector2 _moveDirection;
        private static float _moveUntil;
        private static float _lastPhysicalKeyTime = -10f;
        private static Keyboard _keyboard;
        private static bool _initialized;

        /// <summary>Cantidad de caracteres aceptados (para diagnóstico).</summary>
        public static int AcceptedChars { get; private set; }

        /// <summary>True si hay un pulso de movimiento vigente originado en texto.</summary>
        public static bool TryGetMove(out Vector2 direction)
        {
            Initialize();
            if (Time.unscaledTime < _moveUntil)
            {
                direction = _moveDirection;
                return true;
            }
            direction = Vector2.zero;
            return false;
        }

        /// <summary>True si el carácter se tipeó en este frame (sin distinguir mayúsculas).</summary>
        public static bool Typed(char c)
        {
            Initialize();
            if (_frame != Time.frameCount)
                return false;
            c = char.ToLowerInvariant(c);
            for (int i = 0; i < _count; i++)
            {
                if (Buffer[i] == c)
                    return true;
            }
            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            _initialized = false;
            _keyboard = null;
            _count = 0;
            _frame = -1;
            _moveUntil = 0f;
            AcceptedChars = 0;
        }

        private static void Initialize()
        {
            if (!_initialized)
            {
                _initialized = true;
                InputSystem.onEvent += OnEvent;
                InputSystem.onDeviceChange += (_, __) => Hook();
            }
            Hook();
        }

        private static void Hook()
        {
            var current = Keyboard.current;
            if (current == _keyboard)
                return;
            if (_keyboard != null)
                _keyboard.onTextInput -= OnText;
            _keyboard = current;
            if (_keyboard != null)
                _keyboard.onTextInput += OnText;
        }

        private static void OnEvent(InputEventPtr eventPtr, InputDevice device)
        {
            // Los eventos de estado indican teclas físicas; los de texto no.
            if (device is Keyboard && (eventPtr.type == StateEvent.Type || eventPtr.type == DeltaStateEvent.Type))
                _lastPhysicalKeyTime = Time.unscaledTime;
        }

        private static void OnText(char c)
        {
            // Con un teclado físico el carácter acompaña a la tecla: lo resuelven las acciones.
            if (_keyboard != null && _keyboard.anyKey.isPressed)
                return;
            if (Time.unscaledTime - _lastPhysicalKeyTime < PhysicalGraceSeconds)
                return;

            c = char.ToLowerInvariant(c);
            if (_frame != Time.frameCount)
            {
                _frame = Time.frameCount;
                _count = 0;
            }
            if (_count < BufferSize)
                Buffer[_count++] = c;
            AcceptedChars++;

            var direction = c switch
            {
                'w' => Vector2.up,
                's' => Vector2.down,
                'a' => Vector2.left,
                'd' => Vector2.right,
                _ => Vector2.zero
            };
            if (direction != Vector2.zero)
            {
                _moveDirection = direction;
                _moveUntil = Time.unscaledTime + PulseSeconds;
            }
        }
    }
}
