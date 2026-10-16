using System.Collections.Generic;
using LotoSim.Core;
using LotoSim.Devices;
using LotoSim.Evaluation;
using LotoSim.Interaction;
using LotoSim.Procedure;
using UnityEngine;

namespace LotoSim.Guidance
{
    /// <summary>
    /// Modo guiado: resalta los objetivos de las tareas pendientes, muestra un marcador flotante,
    /// dibuja fantasmas donde se puede colocar el dispositivo LOTO en mano y explica los errores.
    /// En modo evaluación no muestra nada de esto.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GuidanceController : MonoBehaviour
    {
        [SerializeField]
        private ProcedureRunner _runner;

        [SerializeField]
        private LotoKit _kit;

        [SerializeField, Tooltip("Objeto inactivo que se clona como marcador sobre cada objetivo")]
        private GameObject _markerTemplate;

        [SerializeField]
        private Color _pulseColor = new(1f, 0.78f, 0.16f);

        [SerializeField]
        private float _pulseSpeed = 3f;

        private readonly List<HighlightTarget> _targets = new();
        private readonly List<HighlightTarget> _active = new();
        private readonly List<GameObject> _markers = new();
        private readonly HashSet<string> _activeIds = new();
        private LotoSocket[] _sockets;

        private static bool Enabled => SessionContext.IsGuided;

        private void Start()
        {
            _targets.AddRange(FindObjectsByType<HighlightTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            _sockets = FindObjectsByType<LotoSocket>(FindObjectsSortMode.None);
            RefreshTargets();
        }

        private void OnEnable()
        {
            SimEvents.Subscribe<StepStarted>(OnStepStarted);
            SimEvents.Subscribe<TaskCompleted>(OnTaskCompleted);
            SimEvents.Subscribe<ProcedureCompleted>(OnProcedureCompleted);
            SimEvents.Subscribe<MistakeMade>(OnMistakeMade);
            SimEvents.Subscribe<SocketChanged>(OnSocketChanged);
            SimEvents.Subscribe<DeviceStateChanged>(OnDeviceStateChanged);
            if (_kit != null)
                _kit.Changed += RefreshGhosts;
        }

        private void OnDisable()
        {
            SimEvents.Unsubscribe<StepStarted>(OnStepStarted);
            SimEvents.Unsubscribe<TaskCompleted>(OnTaskCompleted);
            SimEvents.Unsubscribe<ProcedureCompleted>(OnProcedureCompleted);
            SimEvents.Unsubscribe<MistakeMade>(OnMistakeMade);
            SimEvents.Unsubscribe<SocketChanged>(OnSocketChanged);
            SimEvents.Unsubscribe<DeviceStateChanged>(OnDeviceStateChanged);
            if (_kit != null)
                _kit.Changed -= RefreshGhosts;
        }

        private void Update()
        {
            if (_active.Count == 0)
                return;
            float intensity = 0.35f + 0.35f * Mathf.Sin(Time.time * _pulseSpeed);
            float bob = Mathf.Sin(Time.time * 2.5f) * 0.04f;
            for (int i = 0; i < _active.Count; i++)
            {
                var target = _active[i];
                if (target == null || !target.isActiveAndEnabled)
                {
                    _markers[i].SetActive(false);
                    continue;
                }
                target.Pulse(_pulseColor, intensity);
                _markers[i].SetActive(true);
                _markers[i].transform.position = target.MarkerPosition() + Vector3.up * bob;
                _markers[i].transform.Rotate(Vector3.up, 90f * Time.deltaTime, Space.World);
            }
        }

        private void OnStepStarted(StepStarted evt) => RefreshTargets();

        private void OnTaskCompleted(TaskCompleted evt) => RefreshTargets();

        private void OnProcedureCompleted(ProcedureCompleted evt)
        {
            SetActiveTargets(null);
            ClearGhosts();
        }

        private void OnSocketChanged(SocketChanged evt) => RefreshGhosts();

        private void OnDeviceStateChanged(DeviceStateChanged evt) => RefreshGhosts();

        private void OnMistakeMade(MistakeMade evt)
        {
            if (!Enabled)
                return;
            var mistake = evt.Mistake;
            bool critical = mistake.Severity == MistakeSeverity.Critical;
            Toasts.Show($"<b>{(critical ? "Error crítico" : "Error")}: {mistake.Title}</b>\n{mistake.Explanation}",
                critical ? ToastKind.Error : ToastKind.Warning);
        }

        private void RefreshTargets()
        {
            _activeIds.Clear();
            var progress = _runner != null ? _runner.Progress : null;
            var step = progress?.CurrentStep;
            if (Enabled && step != null)
            {
                for (int t = 0; t < step.Tasks.Count; t++)
                {
                    if (progress.IsTaskDone(progress.CurrentStepIndex, t))
                        continue;
                    foreach (var id in step.Tasks[t].HighlightIds)
                        _activeIds.Add(id);
                    if (step.Ordered)
                        break;
                }
            }
            SetActiveTargets(_activeIds);
        }

        private void SetActiveTargets(HashSet<string> ids)
        {
            foreach (var target in _active)
            {
                if (target != null)
                    target.SetHighlight(false);
            }
            _active.Clear();

            if (ids != null)
            {
                foreach (var target in _targets)
                {
                    foreach (var id in ids)
                    {
                        if (!target.HasId(id))
                            continue;
                        target.SetHighlight(true);
                        _active.Add(target);
                        break;
                    }
                }
            }

            while (_markers.Count < _active.Count)
                _markers.Add(Instantiate(_markerTemplate, transform));
            for (int i = 0; i < _markers.Count; i++)
                _markers[i].SetActive(i < _active.Count);
        }

        private void RefreshGhosts()
        {
            if (_sockets == null)
                return;
            var selected = _kit != null ? _kit.Selected : LotoItemType.None;
            foreach (var socket in _sockets)
            {
                bool show = Enabled && selected != LotoItemType.None && socket.CanPlace(selected, out _);
                socket.ShowGhost(show ? selected : LotoItemType.None);
            }
        }

        private void ClearGhosts()
        {
            if (_sockets == null)
                return;
            foreach (var socket in _sockets)
                socket.ShowGhost(LotoItemType.None);
        }
    }
}
