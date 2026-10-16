using System;
using System.Collections.Generic;
using LotoSim.Core;
using LotoSim.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LotoSim.Player
{
    /// <summary>
    /// Mapa de estaciones (T): salta con un fundido a puntos clave de la sala.
    /// En VR equivale a anclas de teletransporte.
    /// </summary>
    [RequireComponent(typeof(FirstPersonMotor))]
    [DisallowMultipleComponent]
    public sealed class StationTeleport : MonoBehaviour
    {
        [SerializeField]
        private List<Station> _stations = new();

        private FirstPersonMotor _motor;
        private InputAction _toggle;
        private InputAction[] _slots;

        public IReadOnlyList<Station> Stations => _stations;
        public bool IsOpen => OverlayMenu.IsOpenBy(this);
        public static event Action MenuChanged;

        private void Awake()
        {
            _motor = GetComponent<FirstPersonMotor>();
            var actions = InputSystem.actions;
            _toggle = actions.FindAction("Player/StationMap", true);
            _slots = new[]
            {
                actions.FindAction("Player/Slot1", true),
                actions.FindAction("Player/Slot2", true),
                actions.FindAction("Player/Slot3", true),
                actions.FindAction("Player/Slot4", true),
                actions.FindAction("Player/Slot5", true)
            };
        }

        private void OnDisable() => SetOpen(false);

        private void Update()
        {
            if (GameFlow.State != GameState.Playing || DialogueSession.IsOpen)
            {
                SetOpen(false);
                return;
            }
            if (_toggle.WasPressedThisFrame())
            {
                SetOpen(!IsOpen);
                return;
            }
            if (!IsOpen)
                return;
            for (int i = 0; i < _slots.Length && i < _stations.Count; i++)
            {
                if (_slots[i].WasPressedThisFrame())
                {
                    GoTo(_stations[i]);
                    return;
                }
            }
        }

        private void GoTo(Station station)
        {
            SetOpen(false);
            var point = station.Point;
            ScreenFade.Run(() => _motor.Teleport(point.position, point.eulerAngles.y));
        }

        private void SetOpen(bool open)
        {
            if (open == IsOpen)
                return;
            if (open && !OverlayMenu.TryOpen(this))
                return;
            if (!open)
                OverlayMenu.Close(this);
            MenuChanged?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => MenuChanged = null;
    }

    [Serializable]
    public sealed class Station
    {
        [SerializeField]
        private string _name;

        [SerializeField]
        private Transform _point;

        public string Name => _name;
        public Transform Point => _point;
    }
}
