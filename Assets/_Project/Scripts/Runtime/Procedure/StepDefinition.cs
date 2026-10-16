using System.Collections.Generic;
using UnityEngine;

namespace LotoSim.Procedure
{
    /// <summary>Paso del procedimiento (ej. S3 "Aislar las fuentes de energía").</summary>
    [CreateAssetMenu(menuName = "LotoSim/Procedure Step", fileName = "STEP_New")]
    public sealed class StepDefinition : ScriptableObject
    {
        [SerializeField]
        private string _id;

        [SerializeField]
        private string _title;

        [SerializeField, TextArea(2, 5)]
        private string _description;

        [SerializeField, Tooltip("Si es true, las tareas se completan en orden")]
        private bool _ordered;

        [SerializeField]
        private List<TaskDefinition> _tasks = new();

        public string Id => _id;
        public string Title => _title;
        public string Description => _description;
        public bool Ordered => _ordered;
        public IReadOnlyList<TaskDefinition> Tasks => _tasks;

#if UNITY_EDITOR
        public void EditorInitialize(string id, string title, string description, bool ordered, List<TaskDefinition> tasks)
        {
            _id = id;
            _title = title;
            _description = description;
            _ordered = ordered;
            _tasks = tasks;
        }
#endif
    }
}
