using LotoSim.Core;
using LotoSim.Devices;
using LotoSim.Procedure;
using UnityEngine;

namespace LotoSim.Evaluation
{
    /// <summary>
    /// Reglas de peligro y de orden específicas de la P-101. Corre en los handlers de eventos,
    /// antes de que la bomba recalcule (LateUpdate): ve el estado previo a la acción.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HazardMonitor : MonoBehaviour
    {
        private const string Disconnect = "DS-101";
        private const string Suction = "V-101";
        private const string Discharge = "V-102";
        private const string Bleed = "V-103";

        private const string MaintenanceDone = "maintenance.done";
        private const string NotifyRestore = "notify.restore";

        [SerializeField]
        private AssessmentTracker _tracker;

        [SerializeField]
        private PumpSystem _pump;

        [SerializeField]
        private ProcedureRunner _runner;

        private void OnEnable()
        {
            SimEvents.Subscribe<DeviceStateChanged>(OnDeviceStateChanged);
            SimEvents.Subscribe<SocketChanged>(OnSocketChanged);
            SimEvents.Subscribe<PumpStateChanged>(OnPumpStateChanged);
            SimEvents.Subscribe<FlagRaised>(OnFlagRaised);
            SimEvents.Subscribe<ActionSignal>(OnActionSignal);
        }

        private void OnDisable()
        {
            SimEvents.Unsubscribe<DeviceStateChanged>(OnDeviceStateChanged);
            SimEvents.Unsubscribe<SocketChanged>(OnSocketChanged);
            SimEvents.Unsubscribe<PumpStateChanged>(OnPumpStateChanged);
            SimEvents.Unsubscribe<FlagRaised>(OnFlagRaised);
            SimEvents.Unsubscribe<ActionSignal>(OnActionSignal);
        }

        private void OnDeviceStateChanged(DeviceStateChanged evt)
        {
            CheckPpe();
            bool maintenanceDone = Signals.HasFlag(MaintenanceDone);

            switch (evt.DeviceId)
            {
                case Disconnect when evt.State == DeviceState.Off && _pump.IsRunning:
                    _tracker.Report(MistakeIds.SwitchUnderLoad);
                    break;
                case Disconnect when evt.State == DeviceState.On && maintenanceDone:
                    if (!Signals.HasFlag("tool.wrench") || !Signals.HasFlag("tool.screwdriver"))
                        _tracker.Report(MistakeIds.RestoreTools);
                    if (IsState(Bleed, DeviceState.Open))
                        _tracker.Report(MistakeIds.RestoreBleedOpen);
                    break;
                case Suction or Discharge when evt.State == DeviceState.Closed && _pump.IsRunning:
                    _tracker.Report(MistakeIds.Deadhead);
                    break;
                case Suction when evt.State == DeviceState.Closed && IsState(Discharge, DeviceState.Open):
                    _tracker.Report(MistakeIds.ValveCloseOrder);
                    break;
                case Discharge when evt.State == DeviceState.Open && maintenanceDone && IsState(Suction, DeviceState.Closed):
                    _tracker.Report(MistakeIds.RestoreValveOrder);
                    break;
            }
        }

        private void OnSocketChanged(SocketChanged evt)
        {
            CheckPpe();
            var socket = DeviceRegistry.Get(evt.DeviceId)?.Socket;

            if (evt.Placed)
            {
                if (evt.Item == LotoItemType.Tag && socket != null && !socket.Has(LotoItemType.Padlock))
                    _tracker.Report(MistakeIds.TagWithoutLock);
                return;
            }

            if (!Signals.HasFlag(MaintenanceDone))
            {
                if (evt.Item is LotoItemType.Padlock or LotoItemType.ValveLockout)
                    _tracker.Report(MistakeIds.LockRemovedEarly);
            }
            else if (!Signals.HasFlag(NotifyRestore))
            {
                _tracker.Report(MistakeIds.RestoreNoNotify);
            }
        }

        private void OnPumpStateChanged(PumpStateChanged evt)
        {
            if (evt.IsLeaking)
            {
                _tracker.Report(MistakeIds.PressurizedBleed);
                Toasts.Show("¡Sale agua por la purga! La línea sigue presurizada.", ToastKind.Error);
            }
        }

        private void OnFlagRaised(FlagRaised evt)
        {
            if (evt.Key == "kit.collected")
                CheckPpe();
        }

        private void OnActionSignal(ActionSignal evt)
        {
            if (evt.Key == PumpCasing.MaintenanceStartedSignal && !IsVerified())
                _tracker.Report(MistakeIds.WorkWithoutVerify);
        }

        /// <summary>Verificado = se completó la lectura de 0 bar del paso 6 (luego del try-out).</summary>
        private bool IsVerified() =>
            _runner != null && _runner.Progress != null && _runner.Progress.IsTaskDone("S6.gauge");

        private void CheckPpe()
        {
            if (_tracker.HasReported(MistakeIds.PpeMissing))
                return;
            bool complete = Signals.HasFlag("ppe.helmet") && Signals.HasFlag("ppe.glasses") && Signals.HasFlag("ppe.gloves");
            if (!complete)
                _tracker.Report(MistakeIds.PpeMissing);
        }

        private static bool IsState(string deviceId, DeviceState state)
        {
            var device = DeviceRegistry.Get(deviceId);
            return device != null && device.State == state;
        }
    }
}
