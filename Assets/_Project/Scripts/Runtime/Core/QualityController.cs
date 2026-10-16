using LotoSim.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LotoSim.Core
{
    /// <summary>
    /// Calidad gráfica de la sala: Alta (sombras suaves, post-procesado, FXAA) o Baja (sin sombras
    /// en tiempo real, sin post-procesado). En la build web vigila los FPS al empezar y baja la
    /// calidad sola en equipos lentos (notebooks de oficina con GPU integrada).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QualityController : MonoBehaviour
    {
        [SerializeField, Tooltip("FPS promedio por debajo de los cuales se baja la calidad automáticamente")]
        private float _autoLowFps = 24f;

        [SerializeField, Tooltip("Segundos de juego que se ignoran al inicio (carga de shaders)")]
        private float _warmupSeconds = 3f;

        [SerializeField, Tooltip("Segundos que se mide el promedio de FPS")]
        private float _sampleSeconds = 6f;

        private Light _sun;
        private Volume _post;
        private UniversalAdditionalCameraData _camera;
        private float _playTime;
        private float _sampleTime;
        private int _sampleFrames;
        private bool _autoChecked;

        public bool IsLow { get; private set; }

        private void Awake()
        {
            Services.Register(this);
            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional)
                {
                    _sun = light;
                    break;
                }
            }
            _post = FindFirstObjectByType<Volume>();
            if (Camera.main != null)
                Camera.main.TryGetComponent(out _camera);
            Apply(Settings.LowQuality);
        }

        private void OnDestroy() => Services.Unregister(this);

        public void Apply(bool low)
        {
            IsLow = low;
            if (_sun != null)
            {
                _sun.shadows = low ? LightShadows.None : LightShadows.Soft;
                _sun.shadowStrength = 0.7f;
            }
            if (_post != null)
                _post.enabled = !low;
            if (_camera != null)
            {
                _camera.renderPostProcessing = !low;
                _camera.antialiasing = low ? AntialiasingMode.None : AntialiasingMode.FastApproximateAntialiasing;
            }
        }

        private void Update()
        {
            // En el Editor los FPS dependen del foco de la ventana: no se decide nada ahí.
            if (_autoChecked || IsLow || Application.isEditor || GameFlow.State != GameState.Playing)
                return;

            _playTime += Time.unscaledDeltaTime;
            if (_playTime < _warmupSeconds)
                return;

            _sampleTime += Time.unscaledDeltaTime;
            _sampleFrames++;
            if (_sampleTime < _sampleSeconds)
                return;

            _autoChecked = true;
            float fps = _sampleFrames / _sampleTime;
            if (fps >= _autoLowFps)
                return;

            Apply(true);
            Toasts.Show($"Se activó la calidad baja para mejorar la fluidez ({fps:0} FPS). Podés cambiarla en Pausa.", ToastKind.Info);
        }
    }
}
