using LotoSim.Core;

namespace LotoSim.Procedure
{
    /// <summary>Lo que el procedimiento necesita saber del mundo. Permite testear sin escena.</summary>
    public interface IWorldState
    {
        bool TryGetDeviceState(string deviceId, out DeviceState state);
        bool SocketHas(string deviceId, LotoItemType item);
        bool SocketIsEmpty(string deviceId);
        bool IsPumpRunning { get; }
        bool IsPumpDepressurized { get; }
        bool HasFlag(string key);
    }
}
