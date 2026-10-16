using LotoSim.Core;
using LotoSim.Devices;
using LotoSim.Interaction;
using UnityEngine;

namespace LotoSim.Procedure
{
    /// <summary>
    /// Conecta el procedimiento (datos) con el mundo (eventos) y publica el avance.
    /// Implementa IWorldState consultando los dispositivos registrados y la bomba.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProcedureRunner : MonoBehaviour, IWorldState
    {
        [SerializeField]
        private ProcedureDefinition _procedure;

        [SerializeField]
        private PumpSystem _pump;

        private ProcedureProgress _progress;
        private float _startTime = -1f;

        public ProcedureDefinition Procedure => _procedure;
        public ProcedureProgress Progress => _progress;
        public StepDefinition CurrentStep => _progress?.CurrentStep;
        /// <summary>Tiempo de juego desde que terminó el briefing (no cuenta pausas).</summary>
        public float ElapsedSeconds => _startTime < 0f ? 0f : Time.time - _startTime;

        public bool IsPumpRunning => _pump.IsRunning;
        public bool IsPumpDepressurized => _pump.IsDepressurized;

        private void Awake()
        {
            Signals.ClearFlags();
            Services.Register(this);
            DialogueSource.CurrentStepIdProvider = () => CurrentStep != null ? CurrentStep.Id : null;

            _progress = new ProcedureProgress(_procedure, this);
            _progress.StepStarted += OnStepStarted;
            _progress.TaskCompleted += OnTaskCompleted;
            _progress.Completed += OnCompleted;
        }

        private void OnEnable()
        {
            SimEvents.Subscribe<DeviceStateChanged>(OnDeviceStateChanged);
            SimEvents.Subscribe<SocketChanged>(OnSocketChanged);
            SimEvents.Subscribe<PumpStateChanged>(OnPumpStateChanged);
            SimEvents.Subscribe<FlagRaised>(OnFlagRaised);
            SimEvents.Subscribe<ActionSignal>(OnActionSignal);
            SimEvents.Subscribe<GameStateChanged>(OnGameStateChanged);
        }

        private void Start()
        {
            GameFlow.SetState(GameState.Briefing);
            _progress.Begin();
        }

        private void OnGameStateChanged(GameStateChanged evt)
        {
            if (evt.State == GameState.Playing && _startTime < 0f)
                _startTime = Time.time;
        }

        private void OnDisable()
        {
            SimEvents.Unsubscribe<DeviceStateChanged>(OnDeviceStateChanged);
            SimEvents.Unsubscribe<SocketChanged>(OnSocketChanged);
            SimEvents.Unsubscribe<PumpStateChanged>(OnPumpStateChanged);
            SimEvents.Unsubscribe<FlagRaised>(OnFlagRaised);
            SimEvents.Unsubscribe<ActionSignal>(OnActionSignal);
            SimEvents.Unsubscribe<GameStateChanged>(OnGameStateChanged);
        }

        private void OnDestroy()
        {
            Services.Unregister(this);
            DialogueSource.CurrentStepIdProvider = null;
        }

        public bool TryGetDeviceState(string deviceId, out DeviceState state)
        {
            var device = DeviceRegistry.Get(deviceId);
            state = device != null ? device.State : default;
            return device != null;
        }

        public bool SocketHas(string deviceId, LotoItemType item)
        {
            var socket = DeviceRegistry.Get(deviceId)?.Socket;
            return socket != null && socket.Has(item);
        }

        public bool SocketIsEmpty(string deviceId)
        {
            var socket = DeviceRegistry.Get(deviceId)?.Socket;
            return socket != null && !socket.HasAny;
        }

        public bool HasFlag(string key) => Signals.HasFlag(key);

        private void OnDeviceStateChanged(DeviceStateChanged evt) => _progress.Notify();
        private void OnSocketChanged(SocketChanged evt) => _progress.Notify();
        private void OnPumpStateChanged(PumpStateChanged evt) => _progress.Notify();
        private void OnFlagRaised(FlagRaised evt) => _progress.Notify();
        private void OnActionSignal(ActionSignal evt) => _progress.Notify(evt.Key);

        private void OnStepStarted(int index)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Procedure] Paso {_procedure.Steps[index].Id} iniciado");
#endif
            SimEvents.Publish(new StepStarted(index, _procedure.Steps[index]));
        }

        private void OnTaskCompleted(int stepIndex, int taskIndex) =>
            SimEvents.Publish(new TaskCompleted(stepIndex, taskIndex));

        private void OnCompleted()
        {
            GameFlow.SetState(GameState.Completed);
            SimEvents.Publish(new ProcedureCompleted(ElapsedSeconds));
        }
    }
}
