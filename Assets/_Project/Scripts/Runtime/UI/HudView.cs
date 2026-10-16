using System.Text;
using LotoSim.Core;
using LotoSim.Interaction;
using LotoSim.Procedure;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LotoSim.UI
{
    /// <summary>
    /// HUD de escritorio (greybox): paso actual y tareas, prompt de interacción, kit, avisos,
    /// diálogo, pausa y fin. Sólo lee estado y escucha eventos.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HudView : MonoBehaviour
    {
        private const string DoneColor = "#3BB273";
        private const string PendingColor = "#C9CDD2";
        private const string GuideColor = "#FFC629";

        [Header("Referencias de juego")]
        [SerializeField]
        private DesktopInteractor _interactor;

        [SerializeField]
        private LotoKit _kit;

        [Header("Paso")]
        [SerializeField]
        private TMP_Text _stepTitle;

        [SerializeField]
        private TMP_Text _stepBody;

        [SerializeField]
        private TMP_Text _taskList;

        [Header("Centro")]
        [SerializeField]
        private TMP_Text _prompt;

        [SerializeField]
        private TMP_Text _hint;

        [SerializeField]
        private Graphic _crosshair;

        [Header("Kit y avisos")]
        [SerializeField]
        private TMP_Text _kitBar;

        [SerializeField]
        private GameObject _toastPanel;

        [SerializeField]
        private TMP_Text _toastText;

        [SerializeField]
        private float _toastSeconds = 3.5f;

        [Header("Paneles")]
        [SerializeField]
        private GameObject _dialoguePanel;

        [SerializeField]
        private TMP_Text _dialogueText;

        [SerializeField]
        private GameObject _pausePanel;

        [SerializeField]
        private Button _resumeButton;

        [SerializeField]
        private Button _restartButton;

        [SerializeField]
        private GameObject _completePanel;

        [SerializeField]
        private TMP_Text _completeText;

        [SerializeField]
        private Button _completeRestartButton;

        private readonly StringBuilder _builder = new();
        private float _toastHideTime;

        private void Awake()
        {
            _resumeButton.onClick.AddListener(Resume);
            _restartButton.onClick.AddListener(GameFlow.RestartScene);
            _completeRestartButton.onClick.AddListener(GameFlow.RestartScene);
            _toastPanel.SetActive(false);
            _dialoguePanel.SetActive(false);
            _pausePanel.SetActive(false);
            _completePanel.SetActive(false);
        }

        private void OnEnable()
        {
            SimEvents.Subscribe<StepStarted>(OnStepStarted);
            SimEvents.Subscribe<TaskCompleted>(OnTaskCompleted);
            SimEvents.Subscribe<ProcedureCompleted>(OnProcedureCompleted);
            SimEvents.Subscribe<ToastRequested>(OnToast);
            SimEvents.Subscribe<GameStateChanged>(OnGameStateChanged);
            DialogueSession.Changed += RefreshDialogue;
            if (_kit != null)
                _kit.Changed += RefreshKit;
            RefreshKit();
        }

        private void OnDisable()
        {
            SimEvents.Unsubscribe<StepStarted>(OnStepStarted);
            SimEvents.Unsubscribe<TaskCompleted>(OnTaskCompleted);
            SimEvents.Unsubscribe<ProcedureCompleted>(OnProcedureCompleted);
            SimEvents.Unsubscribe<ToastRequested>(OnToast);
            SimEvents.Unsubscribe<GameStateChanged>(OnGameStateChanged);
            DialogueSession.Changed -= RefreshDialogue;
            if (_kit != null)
                _kit.Changed -= RefreshKit;
        }

        private void Update()
        {
            RefreshPrompt();
            if (_toastPanel.activeSelf && Time.unscaledTime >= _toastHideTime)
                _toastPanel.SetActive(false);
        }

        private void RefreshPrompt()
        {
            bool playing = GameFlow.State == GameState.Playing;
            bool locked = CursorControl.HasControl;

            _hint.gameObject.SetActive(playing && !locked);
            _crosshair.enabled = playing && locked && !DialogueSession.IsOpen;

            var prompt = _interactor != null ? _interactor.Prompt : null;
            if (!playing || !locked || string.IsNullOrEmpty(prompt))
            {
                _prompt.text = string.Empty;
                return;
            }
            bool actionable = _interactor.TargetInReach && _interactor.TargetEnabled;
            string content = actionable
                ? $"<color={GuideColor}>Click</color>  ·  {prompt}"
                : $"<color={PendingColor}>{prompt}</color>";
            _prompt.text = $"<mark=#1C1F24CC padding=\"20,20,8,8\">{content}</mark>";
        }

        private void RefreshSteps()
        {
            var runner = Services.Get<ProcedureRunner>();
            var progress = runner != null ? runner.Progress : null;
            var step = progress?.CurrentStep;
            if (step == null)
                return;

            int index = progress.CurrentStepIndex;
            int last = progress.StepCount - 1;
            string number = index == 0 ? "PREVIO" : $"PASO {index} DE {last}";
            _stepTitle.text = $"<size=70%><color={GuideColor}>{number}</color></size>\n{step.Title}";
            _stepBody.text = SessionContext.IsGuided ? step.Description : string.Empty;

            _builder.Clear();
            for (int t = 0; t < step.Tasks.Count; t++)
            {
                bool done = progress.IsTaskDone(index, t);
                _builder.Append(done ? $"<color={DoneColor}>[x]  " : $"<color={PendingColor}>[  ]  ")
                    .Append(step.Tasks[t].Description)
                    .Append("</color>\n");
            }
            _taskList.text = _builder.ToString();
        }

        private void RefreshKit()
        {
            if (_kit == null || !_kit.IsCollected)
            {
                _kitBar.text = "Kit LOTO: no lo tenés (está en la estación de bloqueo)";
                return;
            }
            _kitBar.text =
                $"{Slot(1, LotoItemType.Padlock)}     {Slot(2, LotoItemType.Tag)}     {Slot(3, LotoItemType.ValveLockout)}" +
                $"     <color={PendingColor}>Llave: sí  ·  R / click der.: guardar</color>";
        }

        private string Slot(int key, LotoItemType type)
        {
            string label = $"[{key}] {LotoItems.Name(type)} x{_kit.Count(type)}";
            return _kit.Selected == type ? $"<color={GuideColor}><b>{label}</b></color>" : label;
        }

        private void RefreshDialogue()
        {
            var dialogue = DialogueSession.Current;
            _dialoguePanel.SetActive(dialogue != null);
            if (dialogue == null)
                return;

            _builder.Clear();
            _builder.Append($"<b><color={GuideColor}>{dialogue.Speaker}</color></b>\n")
                .Append(dialogue.Line)
                .Append("\n\n");
            for (int i = 0; i < dialogue.Options.Count; i++)
                _builder.Append($"<b>[{i + 1}]</b>  {dialogue.Options[i].Text}\n");
            _builder.Append($"\n<size=80%><color={PendingColor}>Elegí con 1-{dialogue.Options.Count} · Esc para cerrar</color></size>");
            _dialogueText.text = _builder.ToString();
        }

        private void OnStepStarted(StepStarted evt) => RefreshSteps();

        private void OnTaskCompleted(TaskCompleted evt) => RefreshSteps();

        private void OnToast(ToastRequested evt)
        {
            string color = evt.Kind switch
            {
                ToastKind.Success => DoneColor,
                ToastKind.Warning => GuideColor,
                ToastKind.Error => "#E5484D",
                _ => "#FFFFFF"
            };
            _toastText.text = $"<color={color}>{evt.Message}</color>";
            _toastPanel.SetActive(true);
            _toastHideTime = Time.unscaledTime + _toastSeconds;
        }

        private void OnGameStateChanged(GameStateChanged evt)
        {
            _pausePanel.SetActive(evt.State == GameState.Paused);
            if (evt.State != GameState.Completed)
                _completePanel.SetActive(false);
        }

        private void OnProcedureCompleted(ProcedureCompleted evt)
        {
            int minutes = Mathf.FloorToInt(evt.ElapsedSeconds / 60f);
            int seconds = Mathf.FloorToInt(evt.ElapsedSeconds % 60f);
            _completeText.text =
                $"<size=130%><b>Procedimiento completado</b></size>\n\n" +
                $"La bomba P-101 volvió a operar de forma controlada.\nTiempo total: {minutes:00}:{seconds:00}";
            _completePanel.SetActive(true);
        }

        private static void Resume()
        {
            GameFlow.SetState(GameState.Playing);
            CursorControl.Lock();
        }
    }
}
