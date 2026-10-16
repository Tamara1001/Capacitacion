using LotoSim.Core;
using UnityEngine;

namespace LotoSim.Audio
{
    /// <summary>Pasos del jugador según la velocidad horizontal del CharacterController.</summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public sealed class Footsteps : MonoBehaviour
    {
        [SerializeField, Tooltip("Metros recorridos entre pasos")]
        private float _stride = 0.75f;

        private CharacterController _controller;
        private float _distance;

        private void Awake() => _controller = GetComponent<CharacterController>();

        private void Update()
        {
            var velocity = _controller.velocity;
            velocity.y = 0f;
            if (!_controller.isGrounded || velocity.sqrMagnitude < 0.05f)
                return;
            _distance += velocity.magnitude * Time.deltaTime;
            if (_distance < _stride)
                return;
            _distance = 0f;
            Services.Get<AudioService>()?.Play2D(SoundId.Footstep);
        }
    }
}
