using System.Text;
using LotoSim.Core;
using LotoSim.Evaluation;
using LotoSim.Procedure;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LotoSim.UI
{
    /// <summary>Reporte final: resultado, puntaje, tiempo, pasos y errores con su explicación.</summary>
    [DisallowMultipleComponent]
    public sealed class ReportView : MonoBehaviour
    {
        [SerializeField]
        private GameObject _panel;

        [SerializeField]
        private TMP_Text _summary;

        [SerializeField]
        private TMP_Text _steps;

        [SerializeField]
        private TMP_Text _mistakes;

        [SerializeField]
        private Button _retryButton;

        [SerializeField]
        private Button _menuButton;

        private readonly StringBuilder _builder = new();

        private void Awake()
        {
            _panel.SetActive(false);
            _retryButton.onClick.AddListener(GameFlow.RestartScene);
            _menuButton.onClick.AddListener(() => GameFlow.LoadScene(GameFlow.MenuScene));
        }

        private void OnEnable() => SimEvents.Subscribe<ReportReady>(OnReportReady);

        private void OnDisable() => SimEvents.Unsubscribe<ReportReady>(OnReportReady);

        private void OnReportReady(ReportReady evt)
        {
            var report = evt.Report;
            _panel.SetActive(true);

            int minutes = Mathf.FloorToInt(report.ElapsedSeconds / 60f);
            int seconds = Mathf.FloorToInt(report.ElapsedSeconds % 60f);
            string verdict = report.Passed
                ? $"<color={HudView.DoneColor}>APROBADO</color>"
                : $"<color={HudView.ErrorColor}>NO APROBADO</color>";
            string mode = report.Mode == GameMode.Guided ? "Modo guiado" : "Modo evaluación";
            _summary.text =
                $"<size=60%><color={HudView.PendingColor}>{mode.ToUpperInvariant()} · {SessionContext.TraineeName}</color></size>\n" +
                $"<size=150%><b>{verdict}</b></size>\n" +
                $"Puntaje <b>{report.Score}</b>/100   ·   Tiempo <b>{minutes:00}:{seconds:00}</b>   ·   " +
                $"Errores críticos <b>{report.CriticalCount}</b>";

            BuildSteps(report);
            BuildMistakes(report);
        }

        private void BuildSteps(SessionReport report)
        {
            var runner = Services.Get<ProcedureRunner>();
            if (runner == null)
                return;
            _builder.Clear();
            _builder.Append("<b>Pasos</b>\n");
            var steps = runner.Procedure.Steps;
            for (int i = 0; i < steps.Count; i++)
            {
                bool clean = !report.StepHasMistakes(i);
                string mark = clean ? $"<color={HudView.DoneColor}>[ok]</color>" : $"<color={HudView.GuideColor}>[!]</color>";
                string number = i == 0 ? "Previo" : $"Paso {i}";
                _builder.Append($"{mark}  {number} · {steps[i].Title}\n");
            }
            _steps.text = _builder.ToString();
        }

        private void BuildMistakes(SessionReport report)
        {
            _builder.Clear();
            if (report.Mistakes.Count == 0)
            {
                _builder.Append($"<b>Errores</b>\n<color={HudView.DoneColor}>Sin errores. ¡Excelente trabajo!</color>");
                _mistakes.text = _builder.ToString();
                return;
            }

            _builder.Append($"<b>Errores ({report.Mistakes.Count})</b>\n");
            foreach (var record in report.Mistakes)
            {
                bool critical = record.Mistake.Severity == MistakeSeverity.Critical;
                string color = critical ? HudView.ErrorColor : HudView.GuideColor;
                string tag = critical ? "CRÍTICO −20" : "Menor −5";
                string step = record.StepIndex <= 0 ? "Previo" : $"Paso {record.StepIndex}";
                _builder.Append($"<color={color}><b>{tag}</b></color>  {record.Mistake.Title} <color={HudView.PendingColor}>({step})</color>\n")
                    .Append($"<size=85%><color={HudView.PendingColor}>{record.Mistake.Explanation}</color></size>\n");
            }
            _mistakes.text = _builder.ToString();
        }
    }
}
