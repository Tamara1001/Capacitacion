using System;
using System.Collections.Generic;
using UnityEngine;

namespace LotoSim.Core
{
    /// <summary>
    /// Bus de eventos tipado. Desacopla mundo, procedimiento, evaluación y UI.
    /// Los eventos son structs inmutables publicados como hechos en pasado.
    /// </summary>
    public static class SimEvents
    {
        private static readonly List<Action> _resetters = new();

        private static class Channel<T>
        {
            public static Action<T> Handlers;

            static Channel() => _resetters.Add(() => Handlers = null);
        }

        public static void Subscribe<T>(Action<T> handler) => Channel<T>.Handlers += handler;

        public static void Unsubscribe<T>(Action<T> handler) => Channel<T>.Handlers -= handler;

        public static void Publish<T>(T evt) => Channel<T>.Handlers?.Invoke(evt);

        // Necesario si las Enter Play Mode Options desactivan el domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAll()
        {
            foreach (var reset in _resetters)
                reset();
        }
    }
}
