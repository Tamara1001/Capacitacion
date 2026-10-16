using UnityEngine;

namespace LotoSim.Devices
{
    /// <summary>
    /// Simulación determinística y sin MonoBehaviour de la bomba (testeable en Edit Mode).
    /// En marcha: 4 bar. Detenida con alguna válvula abierta: presión de línea.
    /// Detenida con ambas válvulas cerradas: queda presión atrapada hasta purgar.
    /// </summary>
    public sealed class PumpModel
    {
        public const float RunningPressure = 4f;
        public const float LinePressure = 2.5f;
        public const float DepressurizedThreshold = 0.05f;
        public const float ApproachRate = 2.5f;
        public const float BleedRate = 1.1f;

        public PumpModel(float initialPressure)
        {
            Pressure = initialPressure;
        }

        public float Pressure { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsEnergized { get; private set; }
        public bool IsLeaking { get; private set; }
        public bool IsDepressurized => Pressure <= DepressurizedThreshold;

        public void Step(float deltaTime, bool energized, bool runCommand, bool suctionOpen, bool dischargeOpen, bool bleedOpen)
        {
            IsEnergized = energized;
            IsRunning = energized && runCommand;
            bool lineConnected = suctionOpen || dischargeOpen;
            IsLeaking = bleedOpen && (IsRunning || lineConnected);

            float target;
            float rate;
            if (IsRunning)
            {
                target = RunningPressure;
                rate = ApproachRate;
            }
            else if (lineConnected)
            {
                target = LinePressure;
                rate = ApproachRate;
            }
            else if (bleedOpen)
            {
                target = 0f;
                rate = BleedRate;
            }
            else
            {
                target = Pressure;
                rate = 0f;
            }
            Pressure = Mathf.MoveTowards(Pressure, target, rate * deltaTime);
        }
    }
}
