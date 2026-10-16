using LotoSim.Core;
using UnityEngine;

namespace LotoSim.Devices
{
    /// <summary>Botonera local START/STOP. Guarda la orden de marcha; la bomba decide si arranca.</summary>
    [DisallowMultipleComponent]
    public sealed class ControlStation : MonoBehaviour
    {
        public const string StartSignal = "control.start";
        public const string StopSignal = "control.stop";

        [SerializeField, Tooltip("La bomba arranca en marcha al iniciar la escena")]
        private bool _runCommandOnStart = true;

        [SerializeField]
        private Renderer _pilotLight;

        [SerializeField]
        private Color _runningColor = new(0.23f, 0.7f, 0.45f);

        [SerializeField]
        private Color _stoppedColor = new(0.9f, 0.28f, 0.3f);

        private MaterialPropertyBlock _block;

        public bool RunCommand { get; private set; }

        private void Awake() => RunCommand = _runCommandOnStart;

        public void PressStart()
        {
            RunCommand = true;
            Signals.Emit(StartSignal);
        }

        public void PressStop()
        {
            RunCommand = false;
            Signals.Emit(StopSignal);
        }

        /// <summary>Sin energía el contactor cae y la orden de marcha se pierde.</summary>
        public void DropRunCommand() => RunCommand = false;

        public void SetPilot(bool running)
        {
            if (_pilotLight == null)
                return;
            _block ??= new MaterialPropertyBlock();
            var color = running ? _runningColor : _stoppedColor;
            _block.SetColor("_BaseColor", color);
            _block.SetColor("_EmissionColor", color * 2f);
            _pilotLight.SetPropertyBlock(_block);
        }
    }
}
