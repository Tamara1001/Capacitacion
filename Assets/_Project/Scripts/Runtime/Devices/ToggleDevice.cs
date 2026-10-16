using LotoSim.Core;
using UnityEngine;

namespace LotoSim.Devices
{
    /// <summary>Dispositivo de dos posiciones con una manija/palanca que rota entre ellas.</summary>
    public abstract class ToggleDevice : Device
    {
        [SerializeField, Tooltip("Pivot de la palanca o manija")]
        private Transform _handle;

        [SerializeField, Tooltip("Rotación local en posición activa (ON / abierta)")]
        private Vector3 _activeEuler;

        [SerializeField, Tooltip("Rotación local en posición inactiva (OFF / cerrada)")]
        private Vector3 _inactiveEuler;

        [SerializeField]
        private bool _startsActive = true;

        [SerializeField]
        private float _animationSeconds = 0.3f;

        private bool _isActive;
        private Coroutine _tween;

        protected bool IsActive => _isActive;

        protected override void Awake()
        {
            base.Awake();
            _isActive = _startsActive;
            if (_handle != null)
                _handle.localRotation = TargetRotation();
        }

        protected override void Operate()
        {
            _isActive = !_isActive;
            if (_handle != null)
            {
                if (_tween != null)
                    StopCoroutine(_tween);
                _tween = Tween.RotateLocal(this, _handle, TargetRotation(), _animationSeconds);
            }
            PublishState();
        }

        private Quaternion TargetRotation() => Quaternion.Euler(_isActive ? _activeEuler : _inactiveEuler);
    }
}
