using LotoSim.Core;
using LotoSim.Interaction;
using UnityEngine;

namespace LotoSim.Devices
{
    /// <summary>Manómetro PI-101: muestra la presión con la aguja y permite "leerlo".</summary>
    [DisallowMultipleComponent]
    public sealed class PressureGauge : MonoBehaviour, IInteractable
    {
        public const string ReadSignal = "gauge.read";
        public const string ReadZeroSignal = "gauge.read.zero";

        [SerializeField]
        private PumpSystem _pump;

        [SerializeField, Tooltip("Pivot de la aguja; rota sobre su eje Z local")]
        private Transform _needle;

        [SerializeField]
        private float _maxPressure = 10f;

        [SerializeField]
        private float _sweepDegrees = 270f;

        public bool CanInteract(IInteractor interactor) => _pump != null;

        public string GetPrompt(IInteractor interactor) => "Leer el manómetro PI-101";

        public void Interact(IInteractor interactor)
        {
            Signals.Emit(ReadSignal);
            if (_pump.IsDepressurized)
                Signals.Emit(ReadZeroSignal);
            Toasts.Show($"PI-101 marca {_pump.Pressure:0.0} bar.");
        }

        private void Update()
        {
            if (_pump == null || _needle == null)
                return;
            float t = Mathf.Clamp01(_pump.Pressure / _maxPressure);
            float angle = _sweepDegrees * 0.5f - t * _sweepDegrees;
            _needle.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
