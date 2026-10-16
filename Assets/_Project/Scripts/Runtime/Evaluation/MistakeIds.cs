namespace LotoSim.Evaluation
{
    /// <summary>Ids de errores del catálogo detectados por código (ver PROCEDURE.md).</summary>
    public static class MistakeIds
    {
        public const string PpeMissing = "PPE_MISSING";
        public const string ValveCloseOrder = "VALVE_ORDER";
        public const string TagWithoutLock = "TAG_WITHOUT_LOCK";
        public const string RestoreNoNotify = "RESTORE_NO_NOTIFY";
        public const string RestoreValveOrder = "RESTORE_VALVE_ORDER";
        public const string RestoreTools = "RESTORE_TOOLS";
        public const string RestoreBleedOpen = "RESTORE_BLEED_OPEN";

        public const string SwitchUnderLoad = "SWITCH_UNDER_LOAD";
        public const string Deadhead = "DEADHEAD";
        public const string PressurizedBleed = "PRESSURIZED_BLEED";
        public const string WorkWithoutVerify = "WORK_WITHOUT_VERIFY";
        public const string LockRemovedEarly = "LOCK_REMOVED_EARLY";
    }
}
