using System;
using System.Collections;
using LotoSim.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LotoSim.UI
{
    /// <summary>Fundido a negro de pantalla completa; implementa <see cref="ScreenFade"/>.</summary>
    [DisallowMultipleComponent]
    public sealed class FadeView : MonoBehaviour
    {
        [SerializeField]
        private Image _overlay;

        [SerializeField]
        private float _fadeSeconds = 0.25f;

        [SerializeField, Tooltip("Tiempo en negro entre la ida y la vuelta")]
        private float _holdSeconds = 0.15f;

        private bool _running;

        private void Awake() => SetAlpha(0f);

        private void OnEnable() => ScreenFade.Handler = Run;

        private void OnDisable()
        {
            if (ScreenFade.Handler == (Action<Action>)Run)
                ScreenFade.Handler = null;
        }

        private void Run(Action midpoint)
        {
            if (_running)
            {
                midpoint?.Invoke();
                return;
            }
            StartCoroutine(FadeRoutine(midpoint));
        }

        private IEnumerator FadeRoutine(Action midpoint)
        {
            _running = true;
            yield return Animate(0f, 1f);
            midpoint?.Invoke();
            yield return new WaitForSecondsRealtime(_holdSeconds);
            yield return Animate(1f, 0f);
            _running = false;
        }

        private IEnumerator Animate(float from, float to)
        {
            for (float t = 0f; t < _fadeSeconds; t += Time.unscaledDeltaTime)
            {
                SetAlpha(Mathf.Lerp(from, to, t / _fadeSeconds));
                yield return null;
            }
            SetAlpha(to);
        }

        private void SetAlpha(float alpha)
        {
            var color = _overlay.color;
            color.a = alpha;
            _overlay.color = color;
            _overlay.enabled = alpha > 0.001f;
            _overlay.raycastTarget = alpha > 0.001f;
        }
    }
}
