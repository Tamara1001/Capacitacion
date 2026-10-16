using System.Collections;
using LotoSim.Core;
using LotoSim.Devices;
using LotoSim.Interaction;
using LotoSim.Procedure;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LotoSim.Tests
{
    /// <summary>
    /// Recorre el procedimiento completo de la P-101 interactuando con los objetos reales
    /// de la escena, sin input: valida cableado de escena + datos + lógica.
    /// </summary>
    public class ProcedureWalkthroughTests
    {
        private sealed class ScriptedInteractor : IInteractor
        {
            public ScriptedInteractor(LotoKit kit, Transform origin)
            {
                Kit = kit;
                ViewOrigin = origin;
            }

            public Transform ViewOrigin { get; }
            public LotoKit Kit { get; }
        }

        private ScriptedInteractor _interactor;
        private ProcedureRunner _runner;

        [UnityTest]
        public IEnumerator FullProcedure_CorrectSequence_CompletesAllSteps()
        {
            yield return SceneManager.LoadSceneAsync("PumpRoom");
            yield return null;

            var kit = Object.FindFirstObjectByType<LotoKit>();
            _interactor = new ScriptedInteractor(kit, kit.transform);
            _runner = Services.Get<ProcedureRunner>();
            var pump = Services.Get<PumpSystem>();
            Assert.IsNotNull(_runner, "Falta ProcedureRunner en la escena");
            Assert.IsTrue(pump.IsRunning, "La bomba debe arrancar en marcha");

            // S0 — EPP
            AssertStep("S0");
            Use("PPE-HELMET");
            Use("PPE-GLASSES");
            Use("PPE-GLOVES");

            // S1 — Preparar
            AssertStep("S1");
            Use("WORK-ORDER");
            DialogueSession.Choose(0);
            Use("PLATE-DS-101");
            Use("PLATE-V-101");
            Use("PLATE-V-102");
            Use("PLATE-V-103");
            Use("LOTO-STATION");
            Assert.IsTrue(kit.IsCollected);

            // S2 — Comunicar y detener
            AssertStep("S2");
            Use("OPERATOR");
            DialogueSession.Choose(1);
            Use("StopButton");
            yield return null;
            Assert.IsFalse(pump.IsRunning);

            // S3 — Aislar
            AssertStep("S3");
            Use("DS-101");
            Use("V-102");
            Use("V-101");
            yield return null;

            // S4 — Bloquear y etiquetar
            AssertStep("S4");
            Place("DS-101", LotoItemType.Padlock, LotoItemType.Tag);
            Place("V-102", LotoItemType.ValveLockout, LotoItemType.Padlock, LotoItemType.Tag);
            Place("V-101", LotoItemType.ValveLockout, LotoItemType.Padlock, LotoItemType.Tag);

            // S5 — Energía almacenada
            AssertStep("S5");
            Use("V-103");
            float timeout = Time.time + 6f;
            while (!pump.IsDepressurized && Time.time < timeout)
                yield return null;
            yield return null;
            Assert.IsTrue(pump.IsDepressurized, "La purga debe llevar la presión a 0");

            // S6 — Verificar
            AssertStep("S6");
            Use("StartButton");
            yield return null;
            Assert.IsFalse(pump.IsRunning, "Con el aislamiento aplicado la bomba no debe arrancar");
            Use("StopButton");
            Use("PI-101");
            Use("P-101");

            // S7 — Finalizar y restablecer
            AssertStep("S7");
            Use("TOOL-WRENCH");
            Use("TOOL-SCREWDRIVER");
            Use("V-103");
            Use("OPERATOR");
            DialogueSession.Choose(0);
            kit.ClearSelection();
            Remove("DS-101", 2);
            Remove("V-102", 3);
            Remove("V-101", 3);
            Use("LOTO-STATION");
            Use("V-101");
            Use("V-102");
            Use("DS-101");
            Use("StartButton");
            yield return null;
            yield return null;

            Assert.IsTrue(pump.IsRunning, "La bomba debe volver a operar");
            Assert.IsTrue(_runner.Progress.IsComplete, "El procedimiento debe completarse");
            Assert.AreEqual(GameState.Completed, GameFlow.State);
        }

        private void AssertStep(string stepId)
        {
            var current = _runner.CurrentStep;
            Assert.IsNotNull(current, $"Se esperaba el paso {stepId} pero el procedimiento terminó");
            Assert.AreEqual(stepId, current.Id, $"Paso actual inesperado: {DescribePending()}");
        }

        private string DescribePending()
        {
            var progress = _runner.Progress;
            var step = progress.CurrentStep;
            if (step == null)
                return "(ninguno)";
            for (int t = 0; t < step.Tasks.Count; t++)
            {
                if (!progress.IsTaskDone(progress.CurrentStepIndex, t))
                    return $"{step.Id} pendiente: {step.Tasks[t].Description}";
            }
            return step.Id;
        }

        private void Use(string objectName)
        {
            var go = GameObject.Find(objectName);
            Assert.IsNotNull(go, $"No existe el objeto '{objectName}'");
            var interactable = go.GetComponent<IInteractable>();
            Assert.IsNotNull(interactable, $"'{objectName}' no es interactuable");
            Assert.IsTrue(interactable.CanInteract(_interactor),
                $"'{objectName}' no permite interactuar: {interactable.GetPrompt(_interactor)}");
            interactable.Interact(_interactor);
        }

        private void Place(string deviceName, params LotoItemType[] items)
        {
            foreach (var item in items)
            {
                if (_interactor.Kit.Selected != item)
                    _interactor.Kit.Select(item);
                Use(deviceName);
                Assert.IsTrue(GameObject.Find(deviceName).GetComponent<LotoSocket>().Has(item),
                    $"No se colocó {item} en {deviceName}");
            }
        }

        private void Remove(string deviceName, int count)
        {
            for (int i = 0; i < count; i++)
                Use(deviceName);
            Assert.IsFalse(GameObject.Find(deviceName).GetComponent<LotoSocket>().HasAny);
        }
    }
}
