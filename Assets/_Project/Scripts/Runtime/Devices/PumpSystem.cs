using LotoSim.Core;
using UnityEngine;

namespace LotoSim.Devices
{
    /// <summary>
    /// Une los dispositivos de la P-101 con la simulación. Recalcula en LateUpdate, así los
    /// handlers de DeviceStateChanged ven el estado de la bomba previo a la acción.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PumpSystem : MonoBehaviour
    {
        [SerializeField]
        private DisconnectSwitch _disconnect;

        [SerializeField]
        private ControlStation _controlStation;

        [SerializeField]
        private Valve _suction;

        [SerializeField]
        private Valve _discharge;

        [SerializeField]
        private Valve _bleed;

        [SerializeField, Tooltip("Acople que gira mientras la bomba está en marcha")]
        private Transform _coupling;

        [SerializeField]
        private float _couplingDegreesPerSecond = 720f;

        private PumpModel _model;
        private bool _lastEnergized;
        private bool _lastRunning;
        private bool _lastDepressurized;
        private bool _lastLeaking;

        public float Pressure => _model.Pressure;
        public bool IsRunning => _model.IsRunning;
        public bool IsEnergized => _model.IsEnergized;
        public bool IsDepressurized => _model.IsDepressurized;
        public bool IsLeaking => _model.IsLeaking;

        private void Awake()
        {
            _model = new PumpModel(PumpModel.RunningPressure);
            Services.Register(this);
        }

        // En Start: los dispositivos ya inicializaron su posición en Awake.
        private void Start()
        {
            Simulate(0f);
            CacheState();
        }

        private void OnDestroy() => Services.Unregister(this);

        private void LateUpdate()
        {
            Simulate(Time.deltaTime);

            if (_coupling != null && _model.IsRunning)
                _coupling.Rotate(Vector3.up, _couplingDegreesPerSecond * Time.deltaTime, Space.Self);

            if (_model.IsLeaking && !_lastLeaking)
                Toasts.Show("¡Sale agua por la purga! La línea sigue presurizada.", ToastKind.Error);

            if (_model.IsEnergized != _lastEnergized || _model.IsRunning != _lastRunning ||
                _model.IsDepressurized != _lastDepressurized)
            {
                SimEvents.Publish(new PumpStateChanged(_model.IsEnergized, _model.IsRunning, _model.IsDepressurized));
            }
            CacheState();
        }

        private void Simulate(float deltaTime)
        {
            bool energized = _disconnect.State == DeviceState.On;
            if (!energized && _controlStation.RunCommand)
            {
                _controlStation.DropRunCommand();
                if (deltaTime > 0f)
                    Toasts.Show("La bomba no arranca: no tiene energía.");
            }

            _model.Step(deltaTime, energized, _controlStation.RunCommand,
                _suction.IsOpen, _discharge.IsOpen, _bleed.IsOpen);
            _controlStation.SetPilot(_model.IsRunning);
        }

        private void CacheState()
        {
            _lastEnergized = _model.IsEnergized;
            _lastRunning = _model.IsRunning;
            _lastDepressurized = _model.IsDepressurized;
            _lastLeaking = _model.IsLeaking;
        }
    }
}
