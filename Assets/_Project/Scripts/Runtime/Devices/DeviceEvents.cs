using LotoSim.Core;

namespace LotoSim.Devices
{
    public readonly struct DeviceStateChanged
    {
        public readonly string DeviceId;
        public readonly DeviceState State;

        public DeviceStateChanged(string deviceId, DeviceState state)
        {
            DeviceId = deviceId;
            State = state;
        }
    }

    public readonly struct SocketChanged
    {
        public readonly string DeviceId;
        public readonly LotoItemType Item;
        public readonly bool Placed;

        public SocketChanged(string deviceId, LotoItemType item, bool placed)
        {
            DeviceId = deviceId;
            Item = item;
            Placed = placed;
        }
    }

    public readonly struct PumpStateChanged
    {
        public readonly bool IsEnergized;
        public readonly bool IsRunning;
        public readonly bool IsDepressurized;
        public readonly bool IsLeaking;

        public PumpStateChanged(bool isEnergized, bool isRunning, bool isDepressurized, bool isLeaking)
        {
            IsEnergized = isEnergized;
            IsRunning = isRunning;
            IsDepressurized = isDepressurized;
            IsLeaking = isLeaking;
        }
    }
}
