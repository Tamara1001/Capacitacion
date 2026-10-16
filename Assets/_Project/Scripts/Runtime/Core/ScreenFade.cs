using System;
using UnityEngine;

namespace LotoSim.Core
{
    /// <summary>
    /// Fundido a negro con una acción en el punto medio. La UI registra el handler;
    /// sin handler (tests, escenas sin HUD) la acción se ejecuta de inmediato.
    /// </summary>
    public static class ScreenFade
    {
        public static Action<Action> Handler { get; set; }

        public static void Run(Action midpoint)
        {
            if (Handler != null)
                Handler(midpoint);
            else
                midpoint?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Handler = null;
    }
}
