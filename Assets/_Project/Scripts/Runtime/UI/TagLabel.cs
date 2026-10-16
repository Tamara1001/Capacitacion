using LotoSim.Core;
using TMPro;
using UnityEngine;

namespace LotoSim.UI
{
    /// <summary>Completa la etiqueta LOTO con el nombre del participante y la orden de trabajo.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class TagLabel : MonoBehaviour
    {
        [SerializeField]
        private string _workOrder = "OT-0427";

        private void OnEnable() => GetComponent<TMP_Text>().text = $"{SessionContext.TraineeName}\n{_workOrder}";
    }
}
