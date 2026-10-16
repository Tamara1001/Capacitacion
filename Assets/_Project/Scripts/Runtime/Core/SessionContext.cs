namespace LotoSim.Core
{
    /// <summary>Datos de la sesión de capacitación en curso.</summary>
    public static class SessionContext
    {
        public static GameMode Mode { get; set; } = GameMode.Guided;
        public static string TraineeName { get; set; } = "Técnico";
        public static bool IsGuided => Mode == GameMode.Guided;
    }
}
