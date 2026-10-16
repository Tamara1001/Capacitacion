namespace LotoSim.Core
{
    /// <summary>Atajo para pedir un mensaje breve en pantalla.</summary>
    public static class Toasts
    {
        public static void Show(string message, ToastKind kind = ToastKind.Info)
        {
            if (!string.IsNullOrEmpty(message))
                SimEvents.Publish(new ToastRequested(message, kind));
        }
    }
}
