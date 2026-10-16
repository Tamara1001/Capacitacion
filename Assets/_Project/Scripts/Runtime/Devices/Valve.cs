using LotoSim.Core;

namespace LotoSim.Devices
{
    /// <summary>Válvula de dos posiciones (esférica de palanca o purga). Bloqueable si tiene LotoSocket.</summary>
    public sealed class Valve : ToggleDevice
    {
        public bool IsOpen => IsActive;
        public override DeviceState State => IsActive ? DeviceState.Open : DeviceState.Closed;
        public override bool IsIsolated => !IsActive;
        public override string IsolateHint => "Primero cerrá la válvula";

        protected override string OperatePrompt =>
            IsActive ? $"Cerrar {DisplayName}" : $"Abrir {DisplayName}";
    }
}
