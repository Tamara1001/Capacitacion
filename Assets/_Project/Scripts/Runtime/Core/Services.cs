using System;
using System.Collections.Generic;
using UnityEngine;

namespace LotoSim.Core
{
    /// <summary>Registro mínimo de servicios de escena. Evita singletons sueltos.</summary>
    public static class Services
    {
        private static readonly Dictionary<Type, object> _services = new();

        public static void Register<T>(T service) where T : class => _services[typeof(T)] = service;

        public static void Unregister<T>(T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var current) && ReferenceEquals(current, service))
                _services.Remove(typeof(T));
        }

        public static T Get<T>() where T : class =>
            _services.TryGetValue(typeof(T), out var service) ? (T)service : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => _services.Clear();
    }
}
