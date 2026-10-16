using LotoSim.Core;

namespace LotoSim.Devices
{
    /// <summary>Seccionador eléctrico con palanca ON/OFF, bloqueable en OFF.</summary>
    public sealed class DisconnectSwitch : ToggleDevice
    {
        public override DeviceState State => IsActive ? DeviceState.On : DeviceState.Off;
        public override bool IsIsolated => !IsActive;
        public override string IsolateHint => "Primero bajá el seccionador a OFF";

        protected override string OperatePrompt =>
            IsActive ? $"Bajar {DisplayName} a OFF" : $"Subir {DisplayName} a ON";
    }
}
