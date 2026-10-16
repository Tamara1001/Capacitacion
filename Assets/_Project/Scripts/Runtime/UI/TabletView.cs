using System.Text;
using LotoSim.Core;
using LotoSim.Devices;
using LotoSim.Interaction;
using LotoSim.Procedure;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LotoSim.UI
{
    /// <summary>
    /// Tablet diegética (Tab): pestañas Pasos, Tarjeta LOTO (estado en vivo de los puntos de energía)
    /// y Orden de trabajo. En desktop se sostiene frente a la cámara; en VR irá en la muñeca.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TabletView : MonoBehaviour
    {
        private static readonly string[] TabNames = { "Pasos", "Tarjeta LOTO", "Orden de trabajo" };

        [SerializeField]
        private GameObject _root;

        [SerializeField]
        private TMP_Text _tabs;

        [SerializeField]
        private TMP_Text _content;

        [SerializeField]
        private Image _statusLight;

        [SerializeField]
        private float _refreshSeconds = 0.25f;

        private readonly StringBuilder _builder = new();
        private InputAction _toggle;
        private InputAction[] _slots;
        private int _tab;
        private float _nextRefresh;

        public bool IsOpen => OverlayMenu.IsOpenBy(this);

        private void Awake()
        {
            var actions = InputSystem.actions;
            _toggle = actions.FindAction("Player/ToggleTablet", true);
            _slots = new[]
            {
                actions.FindAction("Player/Slot1", true),
                actions.FindAction("Player/Slot2", true),
                actions.FindAction("Player/Slot3", true)
            };
            _root.SetActive(false);
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
                SetOpen(!IsOpen);
            if (!IsOpen)
                return;

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].WasPressedThisFrame())
                {
                    _tab = i;
                    _nextRefresh = 0f;
                }
            }
            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + _refreshSeconds;
                Refresh();
            }
        }

        private void SetOpen(bool open)
        {
            if (open == IsOpen)
                return;
            if (open && !OverlayMenu.TryOpen(this))
                return;
            if (!open)
                OverlayMenu.Close(this);
            _root.SetActive(open);
            _nextRefresh = 0f;
        }

        private void Refresh()
        {
            _builder.Clear();
            for (int i = 0; i < TabNames.Length; i++)
            {
                string label = $"[{i + 1}] {TabNames[i]}";
                _builder.Append(i == _tab ? $"<color={HudView.GuideColor}><b>{label}</b></color>" : $"<color={HudView.PendingColor}>{label}</color>");
                _builder.Append("     ");
            }
            _tabs.text = _builder.ToString();

            _content.text = _tab switch
            {
                0 => StepsText(),
                1 => LotoCardText(),
                _ => WorkOrderText()
            };

            var pump = Services.Get<PumpSystem>();
            if (pump != null)
                _statusLight.color = pump.IsRunning ? new Color(0.89f, 0.28f, 0.3f) : new Color(0.23f, 0.7f, 0.45f);
        }

        private string StepsText()
        {
            var runner = Services.Get<ProcedureRunner>();
            if (runner == null)
                return string.Empty;
            var progress = runner.Progress;
            _builder.Clear();
            _builder.Append("<b>Procedimiento LOTO · P-101</b>\n\n");
            var steps = runner.Procedure.Steps;
            for (int i = 0; i < steps.Count; i++)
            {
                string number = i == 0 ? "Previo" : $"Paso {i}";
                if (progress.IsComplete || i < progress.CurrentStepIndex)
                    _builder.Append($"<color={HudView.DoneColor}>[x]  {number} · {steps[i].Title}</color>\n");
                else if (i == progress.CurrentStepIndex)
                    _builder.Append($"<color={HudView.GuideColor}><b>>   {number} · {steps[i].Title}</b></color>\n");
                else
                    _builder.Append($"<color={HudView.PendingColor}>[  ]  {number} · {steps[i].Title}</color>\n");
            }
            return _builder.ToString();
        }

        private string LotoCardText()
        {
            _builder.Clear();
            _builder.Append("<b>Tarjeta de bloqueo · P-101</b>\n\n");
            _builder.Append($"<color={HudView.PendingColor}>Punto<pos=17%>Energía<pos=55%>Estado<pos=78%>LOTO</color>\n");
            Row("DS-101", "Eléctrica 380 V");
            Row("V-101", "Hidráulica (succ.)");
            Row("V-102", "Hidráulica (desc.)");
            Row("V-103", "Presión residual");
            var pump = Services.Get<PumpSystem>();
            if (pump != null)
            {
                string pressure = pump.IsDepressurized
                    ? $"<color={HudView.DoneColor}>{pump.Pressure:0.0} bar</color>"
                    : $"<color={HudView.ErrorColor}>{pump.Pressure:0.0} bar</color>";
                string running = pump.IsRunning ? $"<color={HudView.ErrorColor}>EN MARCHA</color>" : $"<color={HudView.DoneColor}>DETENIDA</color>";
                _builder.Append($"\nPI-101 presión: {pressure}      Bomba: {running}");
            }
            return _builder.ToString();
        }

        private void Row(string id, string energy)
        {
            var device = DeviceRegistry.Get(id);
            if (device == null)
                return;
            string state = device.State switch
            {
                DeviceState.On => $"<color={HudView.ErrorColor}>ON</color>",
                DeviceState.Off => $"<color={HudView.DoneColor}>OFF</color>",
                DeviceState.Open => id == "V-103" ? $"<color={HudView.GuideColor}>ABIERTA</color>" : $"<color={HudView.ErrorColor}>ABIERTA</color>",
                _ => $"<color={HudView.DoneColor}>CERRADA</color>"
            };
            string loto = "—";
            var socket = device.Socket;
            if (socket != null)
            {
                loto = (socket.Has(LotoItemType.ValveLockout) ? "B " : socket.RequiresLockout ? "· " : "")
                       + (socket.Has(LotoItemType.Padlock) ? "C " : "· ")
                       + (socket.Has(LotoItemType.Tag) ? "E" : "·");
                bool complete = socket.Has(LotoItemType.Padlock) && socket.Has(LotoItemType.Tag) &&
                                (!socket.RequiresLockout || socket.Has(LotoItemType.ValveLockout));
                loto = complete ? $"<color={HudView.DoneColor}>{loto}</color>" : loto;
            }
            _builder.Append($"<b>{id}</b><pos=17%>{energy}<pos=55%>{state}<pos=78%>{loto}\n");
        }

        private static string WorkOrderText() =>
            "<b>Orden de trabajo OT-0427</b>\n\n" +
            "<b>Equipo:</b> bomba centrífuga P-101 (agua de proceso)\n" +
            "<b>Trabajo:</b> reemplazo del sello mecánico\n" +
            $"<b>Ejecuta:</b> {SessionContext.TraineeName} (técnico autorizado)\n\n" +
            "<b>Energías a controlar</b>\n" +
            "· Eléctrica: seccionador DS-101 (tablero TG-1)\n" +
            "· Hidráulica: válvulas V-101 (succión) y V-102 (descarga)\n" +
            "· Presión residual: purga V-103, verificar en PI-101\n\n" +
            $"<color={HudView.PendingColor}>B = bloqueo de válvula · C = candado · E = etiqueta</color>";
    }
}
