using LotoSim.Devices;
using NUnit.Framework;

namespace LotoSim.Tests
{
    public class PumpModelTests
    {
        private static void Run(PumpModel model, float seconds, bool energized, bool run, bool suction, bool discharge, bool bleed)
        {
            for (float t = 0f; t < seconds; t += 0.1f)
                model.Step(0.1f, energized, run, suction, discharge, bleed);
        }

        [Test]
        public void Step_EnergizedWithRunCommand_IsRunningAtRunningPressure()
        {
            var model = new PumpModel(0f);
            Run(model, 5f, true, true, true, true, false);
            Assert.IsTrue(model.IsRunning);
            Assert.AreEqual(PumpModel.RunningPressure, model.Pressure, 0.01f);
        }

        [Test]
        public void Step_NotEnergized_DoesNotRunEvenWithRunCommand()
        {
            var model = new PumpModel(PumpModel.RunningPressure);
            Run(model, 1f, false, true, true, true, false);
            Assert.IsFalse(model.IsRunning);
        }

        [Test]
        public void Step_StoppedWithValvesClosed_KeepsTrappedPressure()
        {
            var model = new PumpModel(PumpModel.RunningPressure);
            Run(model, 2f, true, false, true, true, false);
            Run(model, 10f, false, false, false, false, false);
            Assert.AreEqual(PumpModel.LinePressure, model.Pressure, 0.01f);
            Assert.IsFalse(model.IsDepressurized);
        }

        [Test]
        public void Step_IsolatedAndBleeding_Depressurizes()
        {
            var model = new PumpModel(PumpModel.LinePressure);
            Run(model, 10f, false, false, false, false, true);
            Assert.IsTrue(model.IsDepressurized);
            Assert.IsFalse(model.IsLeaking);
        }

        [Test]
        public void Step_BleedingWithValveOpen_LeaksAndStaysPressurized()
        {
            var model = new PumpModel(PumpModel.LinePressure);
            Run(model, 10f, false, false, true, false, true);
            Assert.IsTrue(model.IsLeaking);
            Assert.IsFalse(model.IsDepressurized);
        }
    }
}
