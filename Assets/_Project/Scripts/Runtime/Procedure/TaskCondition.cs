using System;
using LotoSim.Core;
using UnityEngine;

namespace LotoSim.Procedure
{
    public enum ConditionKind
    {
        DeviceState = 0,
        SocketHas = 1,
        SocketEmpty = 2,
        PumpRunning = 3,
        PumpStopped = 4,
        PumpDepressurized = 5,
        Flag = 6,
        Signal = 7
    }

    /// <summary>
    /// Condición evaluable contra el estado del mundo. Plana (sin polimorfismo) para editarse
    /// cómodamente en el Inspector. Flag = hito persistente; Signal = acción durante el paso actual.
    /// </summary>
    [Serializable]
    public struct TaskCondition
    {
        [SerializeField]
        private ConditionKind _kind;

        [SerializeField, Tooltip("DeviceId para dispositivos, o clave para Flag/Signal")]
        private string _target;

        [SerializeField]
        private DeviceState _state;

        [SerializeField]
        private LotoItemType _item;

        public ConditionKind Kind => _kind;
        public string Target => _target;
        public DeviceState State => _state;
        public LotoItemType Item => _item;

        private TaskCondition(ConditionKind kind, string target, DeviceState state = default, LotoItemType item = default)
        {
            _kind = kind;
            _target = target;
            _state = state;
            _item = item;
        }

        public static TaskCondition DeviceIs(string deviceId, DeviceState state) => new(ConditionKind.DeviceState, deviceId, state);
        public static TaskCondition SocketHas(string deviceId, LotoItemType item) => new(ConditionKind.SocketHas, deviceId, item: item);
        public static TaskCondition SocketEmpty(string deviceId) => new(ConditionKind.SocketEmpty, deviceId);
        public static TaskCondition PumpRunning() => new(ConditionKind.PumpRunning, null);
        public static TaskCondition PumpStopped() => new(ConditionKind.PumpStopped, null);
        public static TaskCondition PumpDepressurized() => new(ConditionKind.PumpDepressurized, null);
        public static TaskCondition Flag(string key) => new(ConditionKind.Flag, key);
        public static TaskCondition Signal(string key) => new(ConditionKind.Signal, key);
    }
}
