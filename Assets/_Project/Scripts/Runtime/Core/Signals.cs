using System.Collections.Generic;
using UnityEngine;

namespace LotoSim.Core
{
    /// <summary>
    /// Hitos persistentes (flags) y acciones momentáneas (signals) que alimentan al procedimiento.
    /// Las interacciones simples los emiten sin conocer al procedimiento.
    /// </summary>
    public static class Signals
    {
        private static readonly HashSet<string> _flags = new();

        public static bool HasFlag(string key) => !string.IsNullOrEmpty(key) && _flags.Contains(key);

        public static void RaiseFlag(string key)
        {
            if (string.IsNullOrEmpty(key) || !_flags.Add(key))
                return;
            SimEvents.Publish(new FlagRaised(key));
        }

        public static void Emit(string key)
        {
            if (!string.IsNullOrEmpty(key))
                SimEvents.Publish(new ActionSignal(key));
        }

        public static void ClearFlags() => _flags.Clear();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => _flags.Clear();
    }
}
