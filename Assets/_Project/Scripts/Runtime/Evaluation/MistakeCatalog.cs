using System;
using System.Collections.Generic;
using LotoSim.Core;
using UnityEngine;

namespace LotoSim.Evaluation
{
    /// <summary>
    /// Textos y severidad de todos los errores evaluables. Los errores con <see cref="MistakeDefinition.TriggerSignal"/>
    /// se detectan solos al emitirse esa señal; el resto los detecta <see cref="HazardMonitor"/> por código.
    /// </summary>
    [CreateAssetMenu(menuName = "LotoSim/Mistake Catalog", fileName = "MISTAKES_New")]
    public sealed class MistakeCatalog : ScriptableObject
    {
        [SerializeField]
        private List<MistakeDefinition> _mistakes = new();

        public IReadOnlyList<MistakeDefinition> Mistakes => _mistakes;

        public MistakeDefinition Find(string id)
        {
            foreach (var mistake in _mistakes)
            {
                if (mistake.Id == id)
                    return mistake;
            }
            Debug.LogError($"[Evaluation] Error no definido en el catálogo: {id}", this);
            return null;
        }

#if UNITY_EDITOR
        public void EditorInitialize(List<MistakeDefinition> mistakes) => _mistakes = mistakes;
#endif
    }

    [Serializable]
    public sealed class MistakeDefinition
    {
        [SerializeField]
        private string _id;

        [SerializeField]
        private MistakeSeverity _severity;

        [SerializeField]
        private string _title;

        [SerializeField, TextArea(2, 4)]
        private string _explanation;

        [SerializeField, Tooltip("Señal que dispara el error automáticamente (opcional)")]
        private string _triggerSignal;

        public MistakeDefinition(string id, MistakeSeverity severity, string title, string explanation, string triggerSignal = null)
        {
            _id = id;
            _severity = severity;
            _title = title;
            _explanation = explanation;
            _triggerSignal = triggerSignal;
        }

        public string Id => _id;
        public MistakeSeverity Severity => _severity;
        public string Title => _title;
        public string Explanation => _explanation;
        public string TriggerSignal => _triggerSignal;
    }
}
