namespace LotoSim.Procedure
{
    public readonly struct StepStarted
    {
        public readonly int Index;
        public readonly StepDefinition Step;

        public StepStarted(int index, StepDefinition step)
        {
            Index = index;
            Step = step;
        }
    }

    public readonly struct TaskCompleted
    {
        public readonly int StepIndex;
        public readonly int TaskIndex;

        public TaskCompleted(int stepIndex, int taskIndex)
        {
            StepIndex = stepIndex;
            TaskIndex = taskIndex;
        }
    }

    public readonly struct ProcedureCompleted
    {
        public readonly float ElapsedSeconds;

        public ProcedureCompleted(float elapsedSeconds) => ElapsedSeconds = elapsedSeconds;
    }
}
