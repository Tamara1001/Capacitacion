namespace LotoSim.Core
{
    /// <summary>Un hito persistente de la sesión (ej. "kit.collected").</summary>
    public readonly struct FlagRaised
    {
        public readonly string Key;

        public FlagRaised(string key) => Key = key;
    }

    /// <summary>Una acción momentánea (ej. "control.start"); sólo cuenta para el paso en curso.</summary>
    public readonly struct ActionSignal
    {
        public readonly string Key;

        public ActionSignal(string key) => Key = key;
    }

    public readonly struct ToastRequested
    {
        public readonly string Message;
        public readonly ToastKind Kind;

        public ToastRequested(string message, ToastKind kind)
        {
            Message = message;
            Kind = kind;
        }
    }

    public readonly struct GameStateChanged
    {
        public readonly GameState State;

        public GameStateChanged(GameState state) => State = state;
    }
}
