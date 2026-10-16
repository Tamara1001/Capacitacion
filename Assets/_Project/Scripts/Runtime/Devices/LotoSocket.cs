using System.Collections.Generic;
using LotoSim.Core;
using LotoSim.Interaction;
using UnityEngine;

namespace LotoSim.Devices
{
    /// <summary>
    /// Punto de aislamiento donde se apilan los dispositivos LOTO: bloqueo de válvula → candado → etiqueta.
    /// Se retiran en orden inverso; el candado requiere la llave.
    /// </summary>
    [RequireComponent(typeof(Device))]
    [DisallowMultipleComponent]
    public sealed class LotoSocket : MonoBehaviour
    {
        [SerializeField, Tooltip("Las válvulas de palanca requieren bloqueo de válvula antes del candado")]
        private bool _requiresLockout;

        [SerializeField]
        private GameObject _lockoutVisual;

        [SerializeField]
        private GameObject _padlockVisual;

        [SerializeField]
        private GameObject _tagVisual;

        [SerializeField, Tooltip("Material semitransparente para el contorno fantasma (modo guiado)")]
        private Material _ghostMaterial;

        private readonly Dictionary<LotoItemType, GameObject> _ghosts = new();
        private Device _device;
        private bool _hasLockout;
        private bool _hasPadlock;
        private bool _hasTag;

        public bool HasAny => _hasLockout || _hasPadlock || _hasTag;
        public bool IsLocking => _hasLockout || _hasPadlock;
        public bool RequiresLockout => _requiresLockout;

        public LotoItemType TopItem =>
            _hasTag ? LotoItemType.Tag :
            _hasPadlock ? LotoItemType.Padlock :
            _hasLockout ? LotoItemType.ValveLockout : LotoItemType.None;

        private void Awake()
        {
            _device = GetComponent<Device>();
            RefreshVisuals();
        }

        public bool Has(LotoItemType type) => type switch
        {
            LotoItemType.ValveLockout => _hasLockout,
            LotoItemType.Padlock => _hasPadlock,
            LotoItemType.Tag => _hasTag,
            _ => false
        };

        public bool CanPlace(LotoItemType type, out string reason)
        {
            reason = null;
            switch (type)
            {
                case LotoItemType.ValveLockout:
                    if (!_requiresLockout) reason = "Este punto no usa bloqueo de válvula";
                    else if (_hasLockout) reason = "Ya tiene bloqueo de válvula";
                    else if (!_device.IsIsolated) reason = _device.IsolateHint;
                    break;
                case LotoItemType.Padlock:
                    if (_hasPadlock) reason = "Ya tiene candado";
                    else if (!_device.IsIsolated) reason = _device.IsolateHint;
                    else if (_requiresLockout && !_hasLockout) reason = "Primero colocá el bloqueo de válvula";
                    break;
                case LotoItemType.Tag:
                    if (_hasTag) reason = "Ya tiene etiqueta";
                    break;
                default:
                    reason = "Nada para colocar";
                    break;
            }
            return reason == null;
        }

        public void Place(LotoKit kit, LotoItemType type)
        {
            if (!CanPlace(type, out _) || !kit.TryTake(type))
                return;
            SetItem(type, true);
        }

        public bool CanRemoveTop(LotoKit kit, out string reason)
        {
            reason = null;
            if (kit == null || !kit.IsCollected)
                reason = "Necesitás el kit LOTO para guardar el dispositivo";
            else if (TopItem == LotoItemType.Padlock && !kit.HasKey)
                reason = "Necesitás la llave del candado";
            return reason == null;
        }

        public void RemoveTop(LotoKit kit)
        {
            var top = TopItem;
            if (top == LotoItemType.None || !CanRemoveTop(kit, out _))
                return;
            SetItem(top, false);
            kit.Give(top);
        }

        private void SetItem(LotoItemType type, bool placed)
        {
            switch (type)
            {
                case LotoItemType.ValveLockout: _hasLockout = placed; break;
                case LotoItemType.Padlock: _hasPadlock = placed; break;
                case LotoItemType.Tag: _hasTag = placed; break;
            }
            RefreshVisuals();
            SimEvents.Publish(new SocketChanged(_device.DeviceId, type, placed));
        }

        /// <summary>Muestra el fantasma del dispositivo indicado (None oculta todos).</summary>
        public void ShowGhost(LotoItemType type)
        {
            foreach (var pair in _ghosts)
                pair.Value.SetActive(pair.Key == type);
            if (type == LotoItemType.None || _ghosts.ContainsKey(type) || _ghostMaterial == null)
                return;

            var visual = VisualFor(type);
            if (visual == null)
                return;
            var ghost = Instantiate(visual, visual.transform.parent);
            ghost.name = $"Ghost_{type}";
            ghost.transform.SetLocalPositionAndRotation(visual.transform.localPosition, visual.transform.localRotation);
            ghost.transform.localScale = visual.transform.localScale * 1.08f;
            foreach (var r in ghost.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = _ghostMaterial;
            foreach (var c in ghost.GetComponentsInChildren<Collider>())
                Destroy(c);
            ghost.SetActive(true);
            _ghosts[type] = ghost;
        }

        private GameObject VisualFor(LotoItemType type) => type switch
        {
            LotoItemType.ValveLockout => _lockoutVisual,
            LotoItemType.Padlock => _padlockVisual,
            LotoItemType.Tag => _tagVisual,
            _ => null
        };

        private void RefreshVisuals()
        {
            if (_lockoutVisual != null) _lockoutVisual.SetActive(_hasLockout);
            if (_padlockVisual != null) _padlockVisual.SetActive(_hasPadlock);
            if (_tagVisual != null) _tagVisual.SetActive(_hasTag);
        }
    }
}
