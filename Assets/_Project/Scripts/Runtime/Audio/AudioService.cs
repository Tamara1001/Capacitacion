using LotoSim.Core;
using LotoSim.Devices;
using LotoSim.Evaluation;
using LotoSim.Interaction;
using LotoSim.Procedure;
using UnityEngine;

namespace LotoSim.Audio
{
    /// <summary>
    /// Reproduce los sonidos del simulador reaccionando a los eventos (no hay llamadas de audio
    /// repartidas por la lógica). Pool de fuentes 3D y 2D + ambiente en loop.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioService : MonoBehaviour
    {
        [SerializeField]
        private SoundLibrary _library;

        [SerializeField, Tooltip("Loop de ambiente (ventilación de la sala)")]
        private AudioClip _ambience;

        [SerializeField, Range(0f, 1f)]
        private float _ambienceVolume = 0.35f;

        [SerializeField]
        private int _poolSize = 10;

        private AudioSource[] _pool;
        private int _next;

        private void Awake()
        {
            Services.Register(this);
            _pool = new AudioSource[_poolSize];
            for (int i = 0; i < _poolSize; i++)
            {
                var go = new GameObject($"Voice_{i}");
                go.transform.SetParent(transform, false);
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 1f;
                source.maxDistance = 18f;
                _pool[i] = source;
            }

            if (_ambience != null)
            {
                var ambience = gameObject.AddComponent<AudioSource>();
                ambience.clip = _ambience;
                ambience.loop = true;
                ambience.volume = _ambienceVolume;
                ambience.spatialBlend = 0f;
                ambience.Play();
            }
            AudioListener.volume = Settings.MasterVolume;
        }

        private void OnDestroy() => Services.Unregister(this);

        private void OnEnable()
        {
            SimEvents.Subscribe<DeviceStateChanged>(OnDeviceStateChanged);
            SimEvents.Subscribe<SocketChanged>(OnSocketChanged);
            SimEvents.Subscribe<ActionSignal>(OnActionSignal);
            SimEvents.Subscribe<FlagRaised>(OnFlagRaised);
            SimEvents.Subscribe<ToastRequested>(OnToast);
            SimEvents.Subscribe<StepStarted>(OnStepStarted);
            SimEvents.Subscribe<TaskCompleted>(OnTaskCompleted);
            SimEvents.Subscribe<ReportReady>(OnReportReady);
            SimEvents.Subscribe<MistakeMade>(OnMistakeMade);
            DialogueSession.Changed += OnDialogueChanged;
        }

        private void OnDisable()
        {
            SimEvents.Unsubscribe<DeviceStateChanged>(OnDeviceStateChanged);
            SimEvents.Unsubscribe<SocketChanged>(OnSocketChanged);
            SimEvents.Unsubscribe<ActionSignal>(OnActionSignal);
            SimEvents.Unsubscribe<FlagRaised>(OnFlagRaised);
            SimEvents.Unsubscribe<ToastRequested>(OnToast);
            SimEvents.Unsubscribe<StepStarted>(OnStepStarted);
            SimEvents.Unsubscribe<TaskCompleted>(OnTaskCompleted);
            SimEvents.Unsubscribe<ReportReady>(OnReportReady);
            SimEvents.Unsubscribe<MistakeMade>(OnMistakeMade);
            DialogueSession.Changed -= OnDialogueChanged;
        }

        /// <summary>Sonido en una posición del mundo (3D).</summary>
        public void PlayAt(SoundId id, Vector3 position) => Play(id, position, 1f);

        /// <summary>Sonido sin posición (UI, acciones del propio jugador).</summary>
        public void Play2D(SoundId id) => Play(id, null, 0f);

        private void Play(SoundId id, Vector3? position, float spatialBlend)
        {
            if (_library == null || !_library.TryGet(id, out var entry))
                return;
            var source = _pool[_next];
            _next = (_next + 1) % _pool.Length;
            source.transform.position = position ?? transform.position;
            source.spatialBlend = spatialBlend;
            source.pitch = 1f + Random.Range(-entry.PitchVariance, entry.PitchVariance);
            source.PlayOneShot(entry.PickClip(), entry.Volume);
        }

        private void OnDeviceStateChanged(DeviceStateChanged evt)
        {
            var device = DeviceRegistry.Get(evt.DeviceId);
            if (device == null)
                return;
            PlayAt(device is DisconnectSwitch ? SoundId.SwitchLever : SoundId.ValveTurn, device.transform.position);
        }

        private void OnSocketChanged(SocketChanged evt)
        {
            var device = DeviceRegistry.Get(evt.DeviceId);
            var position = device != null ? device.transform.position : transform.position;
            var id = evt.Item switch
            {
                LotoItemType.Padlock => evt.Placed ? SoundId.PadlockClose : SoundId.PadlockOpen,
                LotoItemType.ValveLockout => SoundId.LockoutAttach,
                _ => SoundId.TagAttach
            };
            PlayAt(id, position);
        }

        private void OnActionSignal(ActionSignal evt)
        {
            if (evt.Key == ControlStation.StartSignal || evt.Key == ControlStation.StopSignal)
                Play2D(SoundId.Button);
            else if (evt.Key == PressureGauge.ReadSignal)
                Play2D(SoundId.UiTick);
        }

        private void OnFlagRaised(FlagRaised evt)
        {
            if (evt.Key.StartsWith("ppe.") || evt.Key.StartsWith("tool.") || evt.Key.StartsWith("kit."))
                Play2D(SoundId.PickUp);
            else if (evt.Key.StartsWith("identify."))
                Play2D(SoundId.UiTick);
        }

        private void OnToast(ToastRequested evt)
        {
            if (evt.Kind == ToastKind.Error)
                Play2D(SoundId.UiError);
            else if (evt.Kind == ToastKind.Warning)
                Play2D(SoundId.UiWarning);
        }

        private void OnStepStarted(StepStarted evt)
        {
            if (evt.Index > 0)
                Play2D(SoundId.UiStepComplete);
        }

        private void OnTaskCompleted(TaskCompleted evt) => Play2D(SoundId.UiConfirm);

        private void OnReportReady(ReportReady evt) => Play2D(SoundId.UiReport);

        private void OnMistakeMade(MistakeMade evt)
        {
            if (evt.Mistake.Id != MistakeIds.SwitchUnderLoad)
                return;
            var device = DeviceRegistry.Get("DS-101");
            if (device != null)
                PlayAt(SoundId.Spark, device.transform.position);
        }

        private void OnDialogueChanged() => Play2D(DialogueSession.IsOpen ? SoundId.UiOpen : SoundId.UiClose);
    }
}
