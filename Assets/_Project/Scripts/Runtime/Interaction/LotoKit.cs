using System;
using System.Collections.Generic;
using LotoSim.Core;
using UnityEngine;

namespace LotoSim.Interaction
{
    /// <summary>
    /// Kit LOTO personal: cantidad de cada dispositivo, ítem seleccionado ("en mano") y llave.
    /// Desktop: barra de slots. VR (futuro): cinturón o caja de herramientas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LotoKit : MonoBehaviour
    {
        private static readonly LotoItemType[] _cycleOrder =
            { LotoItemType.None, LotoItemType.Padlock, LotoItemType.Tag, LotoItemType.ValveLockout };

        [SerializeField, Tooltip("Candados personales en el kit")]
        private int _padlocks = 3;

        [SerializeField, Tooltip("Etiquetas de peligro en el kit")]
        private int _tags = 3;

        [SerializeField, Tooltip("Bloqueos de válvula de palanca en el kit")]
        private int _valveLockouts = 2;

        private readonly Dictionary<LotoItemType, int> _counts = new();

        public bool IsCollected { get; private set; }
        public bool HasKey => IsCollected;
        public LotoItemType Selected { get; private set; }

        /// <summary>True si todos los dispositivos volvieron al kit.</summary>
        public bool IsComplete =>
            Count(LotoItemType.Padlock) == _padlocks &&
            Count(LotoItemType.Tag) == _tags &&
            Count(LotoItemType.ValveLockout) == _valveLockouts;

        public event Action Changed;

        public int Count(LotoItemType type) => _counts.TryGetValue(type, out var count) ? count : 0;

        public int Capacity(LotoItemType type) => type switch
        {
            LotoItemType.Padlock => _padlocks,
            LotoItemType.Tag => _tags,
            LotoItemType.ValveLockout => _valveLockouts,
            _ => 0
        };

        public void Collect()
        {
            IsCollected = true;
            _counts[LotoItemType.Padlock] = _padlocks;
            _counts[LotoItemType.Tag] = _tags;
            _counts[LotoItemType.ValveLockout] = _valveLockouts;
            Selected = LotoItemType.None;
            Changed?.Invoke();
        }

        public void Stow()
        {
            IsCollected = false;
            Selected = LotoItemType.None;
            Changed?.Invoke();
        }

        public void Select(LotoItemType type)
        {
            if (!IsCollected)
            {
                Toasts.Show("Primero tomá el kit LOTO de la estación de bloqueo.");
                return;
            }
            if (type != LotoItemType.None && Count(type) == 0)
            {
                Toasts.Show($"No te quedan: {LotoItems.Name(type)}.", ToastKind.Warning);
                return;
            }
            Selected = Selected == type ? LotoItemType.None : type;
            Changed?.Invoke();
        }

        public void ClearSelection()
        {
            if (Selected == LotoItemType.None)
                return;
            Selected = LotoItemType.None;
            Changed?.Invoke();
        }

        public void Cycle(int direction)
        {
            if (!IsCollected || direction == 0)
                return;
            int index = Array.IndexOf(_cycleOrder, Selected);
            for (int i = 0; i < _cycleOrder.Length; i++)
            {
                index = (index + (direction > 0 ? 1 : -1) + _cycleOrder.Length) % _cycleOrder.Length;
                var candidate = _cycleOrder[index];
                if (candidate == LotoItemType.None || Count(candidate) > 0)
                {
                    Selected = candidate;
                    Changed?.Invoke();
                    return;
                }
            }
        }

        public bool TryTake(LotoItemType type)
        {
            if (!IsCollected || Count(type) <= 0)
                return false;
            _counts[type] = Count(type) - 1;
            if (_counts[type] == 0 && Selected == type)
                Selected = LotoItemType.None;
            Changed?.Invoke();
            return true;
        }

        public void Give(LotoItemType type)
        {
            _counts[type] = Mathf.Min(Count(type) + 1, Capacity(type));
            Changed?.Invoke();
        }
    }
}
