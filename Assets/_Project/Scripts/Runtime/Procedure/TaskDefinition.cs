using System;
using System.Collections.Generic;
using UnityEngine;

namespace LotoSim.Procedure
{
    /// <summary>Tarea de un paso: se completa cuando se cumplen todas sus condiciones.</summary>
    [Serializable]
    public sealed class TaskDefinition
    {
        [SerializeField]
        private string _id;

        [SerializeField, TextArea(1, 3)]
        private string _description;

        [SerializeField]
        private List<TaskCondition> _conditions = new();

        [SerializeField, Tooltip("Ids de objetos a resaltar en modo guiado")]
        private List<string> _highlightIds = new();

        public TaskDefinition(string id, string description, IEnumerable<TaskCondition> conditions, IEnumerable<string> highlightIds)
        {
            _id = id;
            _description = description;
            _conditions = new List<TaskCondition>(conditions);
            _highlightIds = new List<string>(highlightIds);
        }

        public string Id => _id;
        public string Description => _description;
        public IReadOnlyList<TaskCondition> Conditions => _conditions;
        public IReadOnlyList<string> HighlightIds => _highlightIds;
    }
}
