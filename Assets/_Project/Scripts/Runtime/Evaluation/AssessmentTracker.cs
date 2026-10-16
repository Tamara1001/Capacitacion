using System.Collections.Generic;
using LotoSim.Core;
using LotoSim.Procedure;
using UnityEngine;

namespace LotoSim.Evaluation
{
    /// <summary>
    /// Registra errores (uno por tipo) y arma el reporte al completar el procedimiento.
    /// Cualquier sistema reporta errores con <see cref="Report(string)"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AssessmentTracker : MonoBehaviour
    {
        [SerializeField]
        private MistakeCatalog _catalog;

        [SerializeField]
        private ProcedureRunner _runner;

        private readonly List<MistakeRecord> _records = new();
        private readonly HashSet<string> _reported = new();

        public IReadOnlyList<MistakeRecord> Records => _records;
        public SessionReport LastReport { get; private set; }

        private void Awake() => Services.Register(this);

        private void OnDestroy() => Services.Unregister(this);

        private void OnEnable()
        {
            SimEvents.Subscribe<ActionSignal>(OnActionSignal);
            SimEvents.Subscribe<ProcedureCompleted>(OnProcedureCompleted);
        }

        private void OnDisable()
        {
            SimEvents.Unsubscribe<ActionSignal>(OnActionSignal);
            SimEvents.Unsubscribe<ProcedureCompleted>(OnProcedureCompleted);
        }

        public bool HasReported(string mistakeId) => _reported.Contains(mistakeId);

        public void Report(string mistakeId)
        {
            if (_reported.Contains(mistakeId) || GameFlow.State == GameState.Completed)
                return;
            var mistake = _catalog.Find(mistakeId);
            if (mistake == null)
                return;

            _reported.Add(mistakeId);
            int stepIndex = _runner != null && _runner.Progress != null ? _runner.Progress.CurrentStepIndex : -1;
            _records.Add(new MistakeRecord(mistake, stepIndex, _runner != null ? _runner.ElapsedSeconds : 0f));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Evaluation] Error {mistake.Severity}: {mistake.Id}");
#endif
            SimEvents.Publish(new MistakeMade(mistake, stepIndex));
        }

        private void OnActionSignal(ActionSignal evt)
        {
            foreach (var mistake in _catalog.Mistakes)
            {
                if (!string.IsNullOrEmpty(mistake.TriggerSignal) && mistake.TriggerSignal == evt.Key)
                    Report(mistake.Id);
            }
        }

        private void OnProcedureCompleted(ProcedureCompleted evt)
        {
            LastReport = new SessionReport(SessionContext.Mode, evt.ElapsedSeconds, _records.ToArray());
            SimEvents.Publish(new ReportReady(LastReport));
        }
    }
}
