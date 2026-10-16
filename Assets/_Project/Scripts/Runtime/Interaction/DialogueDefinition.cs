using System;
using System.Collections.Generic;
using UnityEngine;

namespace LotoSim.Interaction
{
    /// <summary>Una línea de un personaje (o documento) con opciones de respuesta.</summary>
    [CreateAssetMenu(menuName = "LotoSim/Dialogue", fileName = "DLG_New")]
    public sealed class DialogueDefinition : ScriptableObject
    {
        [SerializeField]
        private string _speaker = "Operador";

        [SerializeField, TextArea(2, 8)]
        private string _line;

        [SerializeField]
        private List<DialogueOption> _options = new();

        public string Speaker => _speaker;
        public string Line => _line;
        public IReadOnlyList<DialogueOption> Options => _options;

#if UNITY_EDITOR
        public void EditorInitialize(string speaker, string line, List<DialogueOption> options)
        {
            _speaker = speaker;
            _line = line;
            _options = options;
        }
#endif
    }

    [Serializable]
    public sealed class DialogueOption
    {
        [SerializeField, TextArea(1, 3)]
        private string _text;

        [SerializeField]
        private bool _isCorrect;

        [SerializeField, Tooltip("Flag que se levanta al elegir esta opción correcta")]
        private string _flag;

        [SerializeField, TextArea(1, 3), Tooltip("Mensaje que se muestra al elegirla")]
        private string _feedback;

        public DialogueOption(string text, bool isCorrect, string flag, string feedback)
        {
            _text = text;
            _isCorrect = isCorrect;
            _flag = flag;
            _feedback = feedback;
        }

        public string Text => _text;
        public bool IsCorrect => _isCorrect;
        public string Flag => _flag;
        public string Feedback => _feedback;
    }
}
