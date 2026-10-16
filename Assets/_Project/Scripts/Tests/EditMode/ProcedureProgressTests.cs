using System.Collections.Generic;
using LotoSim.Core;
using LotoSim.Procedure;
using NUnit.Framework;
using UnityEngine;

namespace LotoSim.Tests
{
    public class ProcedureProgressTests
    {
        private sealed class FakeWorld : IWorldState
        {
            public readonly Dictionary<string, DeviceState> Devices = new();
            public readonly HashSet<string> Flags = new();
            public bool IsPumpRunning { get; set; }
            public bool IsPumpDepressurized { get; set; }

            public bool TryGetDeviceState(string deviceId, out DeviceState state) => Devices.TryGetValue(deviceId, out state);
            public bool SocketHas(string deviceId, LotoItemType item) => false;
            public bool SocketIsEmpty(string deviceId) => true;
            public bool HasFlag(string key) => Flags.Contains(key);
        }

        private readonly List<Object> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created)
                Object.DestroyImmediate(obj);
            _created.Clear();
        }

        private StepDefinition Step(string id, bool ordered, params TaskCondition[] conditions)
        {
            var tasks = new List<TaskDefinition>();
            for (int i = 0; i < conditions.Length; i++)
                tasks.Add(new TaskDefinition($"{id}.{i}", $"Tarea {i}", new[] { conditions[i] }, new string[0]));
            var step = ScriptableObject.CreateInstance<StepDefinition>();
            step.EditorInitialize(id, id, string.Empty, ordered, tasks);
            _created.Add(step);
            return step;
        }

        private ProcedureDefinition Procedure(params StepDefinition[] steps)
        {
            var procedure = ScriptableObject.CreateInstance<ProcedureDefinition>();
            procedure.EditorInitialize("Test", new List<StepDefinition>(steps));
            _created.Add(procedure);
            return procedure;
        }

        [Test]
        public void Notify_AllTasksSatisfied_AdvancesAndCompletes()
        {
            var world = new FakeWorld();
            var progress = new ProcedureProgress(
                Procedure(Step("S0", false, TaskCondition.Flag("a")), Step("S1", false, TaskCondition.Flag("b"))), world);
            bool completed = false;
            progress.Completed += () => completed = true;

            progress.Begin();
            world.Flags.Add("a");
            progress.Notify();
            Assert.AreEqual(1, progress.CurrentStepIndex);

            world.Flags.Add("b");
            progress.Notify();
            Assert.IsTrue(completed);
            Assert.IsTrue(progress.IsComplete);
        }

        [Test]
        public void Notify_FlagsRaisedEarly_CountWhenStepStarts()
        {
            var world = new FakeWorld();
            world.Flags.Add("b");
            var progress = new ProcedureProgress(
                Procedure(Step("S0", false, TaskCondition.Flag("a")), Step("S1", false, TaskCondition.Flag("b"))), world);

            progress.Begin();
            world.Flags.Add("a");
            progress.Notify();

            Assert.IsTrue(progress.IsComplete);
        }

        [Test]
        public void Notify_SignalBeforeItsStep_DoesNotCount()
        {
            var world = new FakeWorld();
            var progress = new ProcedureProgress(
                Procedure(Step("S0", false, TaskCondition.Flag("a")), Step("S1", false, TaskCondition.Signal("go"))), world);

            progress.Begin();
            progress.Notify("go");
            world.Flags.Add("a");
            progress.Notify();
            Assert.AreEqual(1, progress.CurrentStepIndex);

            progress.Notify("go");
            Assert.IsTrue(progress.IsComplete);
        }

        [Test]
        public void Notify_OrderedStep_RequiresTasksInSequence()
        {
            var world = new FakeWorld();
            var progress = new ProcedureProgress(
                Procedure(Step("S0", true, TaskCondition.Signal("start"), TaskCondition.Signal("stop"))), world);

            progress.Begin();
            progress.Notify("stop");
            Assert.IsFalse(progress.IsTaskDone(0, 1), "STOP antes de START no debe contar todavía");

            progress.Notify("start");
            Assert.IsTrue(progress.IsTaskDone(0, 0));
            Assert.IsFalse(progress.IsTaskDone(0, 1), "Un STOP previo a START no cuenta como 'volver a STOP'");

            progress.Notify("stop");
            Assert.IsTrue(progress.IsComplete);
        }

        [Test]
        public void Notify_DeviceStateCondition_TracksWorld()
        {
            var world = new FakeWorld();
            world.Devices["DS-101"] = DeviceState.On;
            var progress = new ProcedureProgress(
                Procedure(Step("S3", false, TaskCondition.DeviceIs("DS-101", DeviceState.Off))), world);

            progress.Begin();
            Assert.IsFalse(progress.IsComplete);

            world.Devices["DS-101"] = DeviceState.Off;
            progress.Notify();
            Assert.IsTrue(progress.IsComplete);
        }
    }
}
