using System.Collections.Generic;
using LotoSim.Core;

namespace LotoSim.Evaluation
{
    /// <summary>Resultado de una sesión. Puntaje base 100: −5 por error menor, −20 por crítico.</summary>
    public sealed class SessionReport
    {
        public const int BaseScore = 100;
        public const int MinorPenalty = 5;
        public const int CriticalPenalty = 20;
        public const int PassingScore = 70;

        public SessionReport(GameMode mode, float elapsedSeconds, IReadOnlyList<MistakeRecord> mistakes)
        {
            Mode = mode;
            ElapsedSeconds = elapsedSeconds;
            Mistakes = mistakes;

            int penalty = 0;
            foreach (var record in mistakes)
            {
                bool critical = record.Mistake.Severity == MistakeSeverity.Critical;
                penalty += critical ? CriticalPenalty : MinorPenalty;
                if (critical)
                    CriticalCount++;
            }
            Score = System.Math.Max(0, BaseScore - penalty);
        }

        public GameMode Mode { get; }
        public float ElapsedSeconds { get; }
        public IReadOnlyList<MistakeRecord> Mistakes { get; }
        public int Score { get; }
        public int CriticalCount { get; }
        public bool Passed => Score >= PassingScore && CriticalCount == 0;

        public bool StepHasMistakes(int stepIndex)
        {
            foreach (var record in Mistakes)
            {
                if (record.StepIndex == stepIndex)
                    return true;
            }
            return false;
        }
    }

    public readonly struct MistakeRecord
    {
        public readonly MistakeDefinition Mistake;
        public readonly int StepIndex;
        public readonly float Time;

        public MistakeRecord(MistakeDefinition mistake, int stepIndex, float time)
        {
            Mistake = mistake;
            StepIndex = stepIndex;
            Time = time;
        }
    }
}
