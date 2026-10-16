using LotoSim.Guidance;
using UnityEngine;

namespace LotoSim.Interaction
{
    /// <summary>
    /// Contorno blanco sobre el objeto interactuable apuntado. Va en el mismo GameObject que el
    /// <see cref="DesktopInteractor"/> (lo agrega el builder de la escena).
    /// </summary>
    [RequireComponent(typeof(DesktopInteractor))]
    [DisallowMultipleComponent]
    public sealed class HoverOutline : MonoBehaviour
    {
        private DesktopInteractor _interactor;
        private HighlightTarget _current;

        private void Awake() => _interactor = GetComponent<DesktopInteractor>();

        private void LateUpdate()
        {
            HighlightTarget next = null;
            if (_interactor.TargetEnabled && _interactor.Target is Component component)
            {
                next = component.GetComponentInParent<HighlightTarget>();
                if (next == null)
                    next = component.gameObject.AddComponent<HighlightTarget>();
            }

            if (next == _current)
                return;
            if (_current != null)
                _current.SetHover(false);
            _current = next;
            if (_current != null)
                _current.SetHover(true);
        }

        private void OnDisable()
        {
            if (_current != null)
                _current.SetHover(false);
            _current = null;
        }
    }
}
