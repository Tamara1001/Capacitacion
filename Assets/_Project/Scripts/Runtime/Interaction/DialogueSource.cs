using System;
using System.Collections.Generic;
using UnityEngine;

namespace LotoSim.Interaction
{
    /// <summary>
    /// Abre un diálogo según el paso del procedimiento en curso (operador, radio, orden de trabajo).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DialogueSource : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private string _prompt = "Hablar con el operador";

        [SerializeField, Tooltip("Diálogos específicos por id de paso (ej. S2)")]
        private List<StepDialogue> _stepDialogues = new();

        [SerializeField, Tooltip("Diálogo cuando ningún paso coincide")]
        private DialogueDefinition _fallback;

        /// <summary>Lo provee el procedimiento; evita que Interaction dependa de Procedure.</summary>
        public static Func<string> CurrentStepIdProvider { get; set; }

        public bool CanInteract(IInteractor interactor) => Resolve() != null;

        public string GetPrompt(IInteractor interactor) => _prompt;

        public void Interact(IInteractor interactor)
        {
            var dialogue = Resolve();
            if (dialogue != null)
                DialogueSession.Open(dialogue);
        }

        private DialogueDefinition Resolve()
        {
            var stepId = CurrentStepIdProvider?.Invoke();
            foreach (var entry in _stepDialogues)
            {
                if (entry.StepId == stepId && entry.Dialogue != null)
                    return entry.Dialogue;
            }
            return _fallback;
        }
    }

    [Serializable]
    public sealed class StepDialogue
    {
        [SerializeField]
        private string _stepId;

        [SerializeField]
        private DialogueDefinition _dialogue;

        public string StepId => _stepId;
        public DialogueDefinition Dialogue => _dialogue;
    }
}
