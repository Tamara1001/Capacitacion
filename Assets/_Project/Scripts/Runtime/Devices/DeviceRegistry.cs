using System.Collections.Generic;
using UnityEngine;

namespace LotoSim.Devices
{
    /// <summary>Resuelve dispositivos de escena por DeviceId (los ScriptableObjects no pueden referenciar la escena).</summary>
    public static class DeviceRegistry
    {
        private static readonly Dictionary<string, Device> _devices = new();

        public static void Register(Device device)
        {
            if (string.IsNullOrEmpty(device.DeviceId))
            {
                Debug.LogError("[Devices] Dispositivo sin DeviceId", device);
                return;
            }
            if (_devices.TryGetValue(device.DeviceId, out var existing) && existing != device)
                Debug.LogError($"[Devices] DeviceId duplicado: {device.DeviceId}", device);
            _devices[device.DeviceId] = device;
        }

        public static void Unregister(Device device)
        {
            if (_devices.TryGetValue(device.DeviceId ?? string.Empty, out var existing) && existing == device)
                _devices.Remove(device.DeviceId);
        }

        public static Device Get(string deviceId) =>
            deviceId != null && _devices.TryGetValue(deviceId, out var device) ? device : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => _devices.Clear();
    }
}
