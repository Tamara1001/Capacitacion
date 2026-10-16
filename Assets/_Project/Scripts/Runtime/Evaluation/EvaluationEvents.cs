namespace LotoSim.Evaluation
{
    /// <summary>Se cometió un error evaluable (se publica una sola vez por tipo de error).</summary>
    public readonly struct MistakeMade
    {
        public readonly MistakeDefinition Mistake;
        public readonly int StepIndex;

        public MistakeMade(MistakeDefinition mistake, int stepIndex)
        {
            Mistake = mistake;
            StepIndex = stepIndex;
        }
    }

    /// <summary>El procedimiento terminó y el reporte está listo.</summary>
    public readonly struct ReportReady
    {
        public readonly SessionReport Report;

        public ReportReady(SessionReport report) => Report = report;
    }
}
