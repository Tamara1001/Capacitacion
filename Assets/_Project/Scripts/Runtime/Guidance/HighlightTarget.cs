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
        private static readonly int OutlineColorId = Shader.PropertyToID("_Color");
        private const string OutlineMaterialName = "M_Outline";

        [SerializeField, Tooltip("Ids por los que la guía encuentra este objeto. Vacío = nombre del GameObject")]
        private List<string> _ids = new();

        [SerializeField, Tooltip("Altura extra del marcador sobre el objeto (m)")]
        private float _markerOffset = 0.18f;

        private readonly List<Renderer> _renderers = new();
        private MaterialPropertyBlock _block;
        private MaterialPropertyBlock _outlineBlock;
        private List<MeshRenderer> _outlines;
        private Color _highlightColor = new(1f, 0.78f, 0.16f);
        private bool _hover;

        public IReadOnlyList<string> Ids => _ids;
        public bool IsHighlighted { get; private set; }

        private void Awake()
        {
            // Sólo la geometría propia: sin textos, sin dispositivos LOTO colocados, fantasmas ni la luz piloto.
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r.GetComponent<TMP_Text>() == null && !IsExcluded(r.transform))
                    _renderers.Add(r);
            }
        }

        private bool IsExcluded(Transform t)
        {
            for (; t != null && t != transform; t = t.parent)
            {
                if (t.name.StartsWith("LOTO_") || t.name.StartsWith("Ghost_") || t.name == "Pilot")
                    return true;
            }
            return false;
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
            RefreshOutline();
        }

        /// <summary>Contorno blanco fino cuando el jugador apunta al objeto (DESIGN.md §Feedback).</summary>
        public void SetHover(bool hover)
        {
            if (_hover == hover)
                return;
            _hover = hover;
            RefreshOutline();
        }

        /// <summary>Aplica la intensidad del pulso (0..1) si está resaltado.</summary>
        public void Pulse(Color color, float intensity)
        {
            if (!IsHighlighted)
                return;
            ApplyEmission(color * intensity);
            _highlightColor = color;
            RefreshOutline();
        }

        private void RefreshOutline()
        {
            bool visible = IsHighlighted || _hover;
            if (visible && _outlines == null)
                BuildOutlines();
            if (_outlines == null)
                return;
            var color = IsHighlighted ? _highlightColor : Color.white;
            _outlineBlock ??= new MaterialPropertyBlock();
            _outlineBlock.SetColor(OutlineColorId, color);
            foreach (var o in _outlines)
            {
                if (o == null)
                    continue;
                o.enabled = visible;
                o.SetPropertyBlock(_outlineBlock);
            }
        }

        private void BuildOutlines()
        {
            _outlines = new List<MeshRenderer>();
            var material = Resources.Load<Material>(OutlineMaterialName);
            if (material == null)
                return;
            foreach (var r in _renderers)
            {
                if (r is not MeshRenderer || !r.TryGetComponent<MeshFilter>(out var filter) || filter.sharedMesh == null)
                    continue;
                var go = new GameObject("Outline") { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(r.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var outline = go.AddComponent<MeshRenderer>();
                var materials = new Material[filter.sharedMesh.subMeshCount];
                for (int i = 0; i < materials.Length; i++)
                    materials[i] = material;
                outline.sharedMaterials = materials;
                outline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                outline.receiveShadows = false;
                outline.enabled = false;
                _outlines.Add(outline);
            }
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
