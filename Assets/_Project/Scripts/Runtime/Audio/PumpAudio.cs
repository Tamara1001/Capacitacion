using LotoSim.Devices;
using UnityEngine;

namespace LotoSim.Audio
{
    /// <summary>Loops de la bomba: zumbido del motor, drenaje por la purga y chorro cuando hay pérdida.</summary>
    [DisallowMultipleComponent]
    public sealed class PumpAudio : MonoBehaviour
    {
        [SerializeField]
        private PumpSystem _pump;

        [SerializeField]
        private Valve _bleed;

        [SerializeField]
        private AudioSource _motor;

        [SerializeField]
        private AudioSource _drain;

        [SerializeField]
        private AudioSource _spray;

        [SerializeField, Tooltip("Velocidad de fundido de volumen (por segundo)")]
        private float _fadeSpeed = 1.5f;

        [SerializeField]
        private float _motorVolume = 0.55f;

        private void Start()
        {
            foreach (var source in new[] { _motor, _drain, _spray })
            {
                source.loop = true;
                source.volume = 0f;
                source.spatialBlend = 1f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 1.5f;
                source.maxDistance = 14f;
                source.Play();
            }
        }

        private void Update()
        {
            float motorTarget = _pump.IsRunning ? _motorVolume : 0f;
            _motor.volume = Mathf.MoveTowards(_motor.volume, motorTarget, _fadeSpeed * 0.5f * Time.deltaTime);
            _motor.pitch = Mathf.Lerp(0.6f, 1f, _motor.volume / Mathf.Max(0.01f, _motorVolume));

            bool draining = _bleed.IsOpen && !_pump.IsLeaking && _pump.Pressure > 0.05f;
            _drain.volume = Mathf.MoveTowards(_drain.volume, draining ? 0.45f * Mathf.Clamp01(_pump.Pressure) + 0.15f : 0f, _fadeSpeed * Time.deltaTime);
            _spray.volume = Mathf.MoveTowards(_spray.volume, _pump.IsLeaking ? 0.8f : 0f, _fadeSpeed * 2f * Time.deltaTime);
        }
    }
}
