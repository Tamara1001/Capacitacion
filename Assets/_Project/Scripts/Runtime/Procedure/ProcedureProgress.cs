using System;
using System.Collections.Generic;

namespace LotoSim.Procedure
{
    /// <summary>
    /// Avance del procedimiento, en C# puro. Las tareas de estado se evalúan contra el mundo;
    /// las señales sólo cuentan si ocurren durante el paso en curso. Una tarea completada queda completada.
    /// </summary>
    public sealed class ProcedureProgress
    {
        private readonly ProcedureDefinition _definition;
        private readonly IWorldState _world;
        private readonly bool[][] _done;
        private readonly HashSet<string> _stepSignals = new();

        public ProcedureProgress(ProcedureDefinition definition, IWorldState world)
        {
            _definition = definition;
            _world = world;
            _done = new bool[definition.Steps.Count][];
            for (int i = 0; i < _done.Length; i++)
                _done[i] = new bool[definition.Steps[i].Tasks.Count];
        }

        public int CurrentStepIndex { get; private set; } = -1;
        public bool IsStarted => CurrentStepIndex >= 0;
        public bool IsComplete { get; private set; }
        public int StepCount => _definition.Steps.Count;

        public StepDefinition CurrentStep =>
            IsStarted && !IsComplete ? _definition.Steps[CurrentStepIndex] : null;

        public event Action<int> StepStarted;
        public event Action<int, int> TaskCompleted;
        public event Action Completed;

        public bool IsTaskDone(int stepIndex, int taskIndex) => _done[stepIndex][taskIndex];

        public void Begin()
        {
            if (IsStarted)
                return;
            CurrentStepIndex = 0;
            StepStarted?.Invoke(0);
            Evaluate();
        }

        /// <summary>Re-evalúa tras un cambio del mundo; opcionalmente registra una señal momentánea.</summary>
        public void Notify(string signalKey = null)
        {
            if (!IsStarted || IsComplete)
                return;
            if (!string.IsNullOrEmpty(signalKey))
                _stepSignals.Add(signalKey);
            Evaluate();
        }

        private void Evaluate()
        {
            while (!IsComplete)
            {
                var step = CurrentStep;
                bool allDone = true;
                for (int t = 0; t < step.Tasks.Count; t++)
                {
                    if (_done[CurrentStepIndex][t])
                        continue;
                    if (IsSatisfied(step.Tasks[t]))
                    {
                        _done[CurrentStepIndex][t] = true;
                        // En pasos ordenados, las señales previas no cuentan para la tarea siguiente.
                        if (step.Ordered)
                            _stepSignals.Clear();
                        TaskCompleted?.Invoke(CurrentStepIndex, t);
                        continue;
                    }
                    allDone = false;
                    if (step.Ordered)
                        break;
                }
                if (!allDone)
                    return;
                AdvanceStep();
            }
        }

        private void AdvanceStep()
        {
            _stepSignals.Clear();
            if (CurrentStepIndex + 1 >= StepCount)
            {
                IsComplete = true;
                Completed?.Invoke();
                return;
            }
            CurrentStepIndex++;
            StepStarted?.Invoke(CurrentStepIndex);
        }

        private bool IsSatisfied(TaskDefinition task)
        {
            foreach (var condition in task.Conditions)
            {
                if (!IsSatisfied(condition))
                    return false;
            }
            return true;
        }

        private bool IsSatisfied(TaskCondition c) => c.Kind switch
        {
            ConditionKind.DeviceState => _world.TryGetDeviceState(c.Target, out var state) && state == c.State,
            ConditionKind.SocketHas => _world.SocketHas(c.Target, c.Item),
            ConditionKind.SocketEmpty => _world.SocketIsEmpty(c.Target),
            ConditionKind.PumpRunning => _world.IsPumpRunning,
            ConditionKind.PumpStopped => !_world.IsPumpRunning,
            ConditionKind.PumpDepressurized => _world.IsPumpDepressurized,
            ConditionKind.Flag => _world.HasFlag(c.Target),
            ConditionKind.Signal => _stepSignals.Contains(c.Target),
            _ => false
        };
    }
}
