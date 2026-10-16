using System.Collections.Generic;
using UnityEngine;

namespace LotoSim.Procedure
{
    /// <summary>Procedimiento completo: lista ordenada de pasos.</summary>
    [CreateAssetMenu(menuName = "LotoSim/Procedure", fileName = "PROC_New")]
    public sealed class ProcedureDefinition : ScriptableObject
    {
        [SerializeField]
        private string _title;

        [SerializeField]
        private List<StepDefinition> _steps = new();

        public string Title => _title;
        public IReadOnlyList<StepDefinition> Steps => _steps;

#if UNITY_EDITOR
        public void EditorInitialize(string title, List<StepDefinition> steps)
        {
            _title = title;
            _steps = steps;
        }
#endif
    }
}
