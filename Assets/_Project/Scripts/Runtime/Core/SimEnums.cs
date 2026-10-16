namespace LotoSim.Core
{
    public enum DeviceState
    {
        Off = 0,
        On = 1,
        Closed = 2,
        Open = 3
    }

    public enum LotoItemType
    {
        None = 0,
        Padlock = 1,
        Tag = 2,
        ValveLockout = 3
    }

    public enum ToastKind
    {
        Info = 0,
        Success = 1,
        Warning = 2,
        Error = 3
    }

    public enum GameMode
    {
        Guided = 0,
        Assessment = 1
    }

    public enum GameState
    {
        Playing = 0,
        Paused = 1,
        Completed = 2
    }
}
