using System;
using LotoSim.Core;
using UnityEngine;

namespace LotoSim.Interaction
{
    /// <summary>Diálogo abierto actualmente. Mientras está abierto se bloquea el movimiento.</summary>
    public static class DialogueSession
    {
        public static DialogueDefinition Current { get; private set; }
        public static bool IsOpen => Current != null;

        public static event Action Changed;

        public static void Open(DialogueDefinition dialogue)
        {
            Current = dialogue;
            Changed?.Invoke();
        }

        public static void Choose(int index)
        {
            if (!IsOpen || index < 0 || index >= Current.Options.Count)
                return;
            var dialogueId = Current.name;
            var option = Current.Options[index];
            Close();
            if (option.IsCorrect)
            {
                Signals.RaiseFlag(option.Flag);
                Toasts.Show(option.Feedback, ToastKind.Success);
            }
            else
            {
                Signals.Emit($"dialogue.wrong.{dialogueId}");
                Toasts.Show(option.Feedback, ToastKind.Warning);
            }
        }

        public static void Close()
        {
            if (!IsOpen)
                return;
            Current = null;
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Current = null;
            Changed = null;
        }
    }
}
