using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LotoSim.Guidance
{
    /// <summary>
    /// Objeto que la guía puede resaltar. Se identifica por uno o más ids (los mismos que usan
    /// las tareas en <c>HighlightIds</c>). El resaltado es un pulso de emisión en sus renderers.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HighlightTarget : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField, Tooltip("Ids por los que la guía encuentra este objeto. Vacío = nombre del GameObject")]
        private List<string> _ids = new();

        [SerializeField, Tooltip("Altura extra del marcador sobre el objeto (m)")]
        private float _markerOffset = 0.18f;

        private readonly List<Renderer> _renderers = new();
        private MaterialPropertyBlock _block;

        public IReadOnlyList<string> Ids => _ids;
        public bool IsHighlighted { get; private set; }

        private void Awake()
        {
            // Sólo la geometría propia: sin textos, sin dispositivos LOTO colocados ni fantasmas.
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r.GetComponent<TMP_Text>() != null || r.name.StartsWith("LOTO_") ||
                    r.name.StartsWith("Ghost_") || r.name == "Pilot")
                    continue;
                _renderers.Add(r);
            }
        }

        /// <summary>Sin ids configurados, el id es el nombre del GameObject (ej. "DS-101").</summary>
        public bool HasId(string id) => _ids.Count == 0 ? name == id : _ids.Contains(id);

        /// <summary>Punto sobre el objeto donde va el marcador.</summary>
        public Vector3 MarkerPosition()
        {
            if (_renderers.Count == 0)
                return transform.position + Vector3.up * _markerOffset;
            var bounds = _renderers[0].bounds;
            for (int i = 1; i < _renderers.Count; i++)
                bounds.Encapsulate(_renderers[i].bounds);
            return new Vector3(bounds.center.x, bounds.max.y + _markerOffset, bounds.center.z);
        }

        public void SetHighlight(bool highlighted)
        {
            IsHighlighted = highlighted;
            if (!highlighted)
                ApplyEmission(Color.black);
        }

        /// <summary>Aplica la intensidad del pulso (0..1) si está resaltado.</summary>
        public void Pulse(Color color, float intensity)
        {
            if (IsHighlighted)
                ApplyEmission(color * intensity);
        }

        private void ApplyEmission(Color color)
        {
            _block ??= new MaterialPropertyBlock();
            foreach (var r in _renderers)
            {
                if (r == null)
                    continue;
                r.GetPropertyBlock(_block);
                _block.SetColor(EmissionColorId, color);
                r.SetPropertyBlock(_block);
            }
        }
    }
}
