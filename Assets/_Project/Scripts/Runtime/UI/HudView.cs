using System.Collections.Generic;
using System.Text;
using LotoSim.Core;
using LotoSim.Interaction;
using LotoSim.Player;
using LotoSim.Procedure;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LotoSim.UI
{
    /// <summary>
    /// HUD de escritorio: paso actual y tareas, prompt de interacción, kit, avisos, diálogo,
    /// mapa de estaciones y pausa con opciones. Sólo lee estado y escucha eventos.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HudView : MonoBehaviour
    {
        public const string DoneColor = "#3BB273";
        public const string PendingColor = "#C9CDD2";
        public const string GuideColor = "#FFC629";
        public const string ErrorColor = "#E5484D";

        [Header("Referencias de juego")]
        [SerializeField]
        private DesktopInteractor _interactor;

        [SerializeField]
        private LotoKit _kit;

        [SerializeField]
        private DesktopLook _look;

        [SerializeField]
        private StationTeleport _stations;

        [Header("Paso")]
        [SerializeField]
        private GameObject _stepPanel;

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

        [Header("Kit, avisos y efectos")]
        [SerializeField]
        private TMP_Text _kitBar;

        [SerializeField]
        private GameObject _toastPanel;

        [SerializeField]
        private TMP_Text _toastText;

        [SerializeField]
        private Image _vignette;

        [Header("Paneles")]
        [SerializeField]
        private GameObject _dialoguePanel;

        [SerializeField]
        private TMP_Text _dialogueText;

        [SerializeField]
        private GameObject _stationPanel;

        [SerializeField]
        private TMP_Text _stationText;

        [Header("Pausa y opciones")]
        [SerializeField]
        private GameObject _pausePanel;

        [SerializeField]
        private Button _resumeButton;

        [SerializeField]
        private Button _restartButton;

        [SerializeField]
        private Button _menuButton;

        [SerializeField]
        private Slider _sensitivitySlider;

        [SerializeField]
        private Toggle _invertToggle;

        private const int MaxToasts = 3;

        private readonly StringBuilder _builder = new();
        private readonly List<(string text, float expires)> _toasts = new();
        private float _vignetteAlpha;

        private void Awake()
        {
            _resumeButton.onClick.AddListener(Resume);
            _restartButton.onClick.AddListener(GameFlow.RestartScene);
            _menuButton.onClick.AddListener(() => GameFlow.LoadScene(GameFlow.MenuScene));

            _sensitivitySlider.minValue = 0.02f;
            _sensitivitySlider.maxValue = 0.5f;
            _sensitivitySlider.SetValueWithoutNotify(Settings.LookSensitivity);
            _sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            _invertToggle.SetIsOnWithoutNotify(Settings.InvertY);
            _invertToggle.onValueChanged.AddListener(OnInvertChanged);

            _toastPanel.SetActive(false);
            _dialoguePanel.SetActive(false);
            _stationPanel.SetActive(false);
            _pausePanel.SetActive(false);
            SetVignette(0f);
        }

        private void OnEnable()
        {
            SimEvents.Subscribe<StepStarted>(OnStepStarted);
            SimEvents.Subscribe<TaskCompleted>(OnTaskCompleted);
            SimEvents.Subscribe<ToastRequested>(OnToast);
            SimEvents.Subscribe<GameStateChanged>(OnGameStateChanged);
            DialogueSession.Changed += RefreshDialogue;
            StationTeleport.MenuChanged += RefreshStations;
            if (_kit != null)
                _kit.Changed += RefreshKit;
            RefreshKit();
        }

        private void OnDisable()
        {
            SimEvents.Unsubscribe<StepStarted>(OnStepStarted);
            SimEvents.Unsubscribe<TaskCompleted>(OnTaskCompleted);
            SimEvents.Unsubscribe<ToastRequested>(OnToast);
            SimEvents.Unsubscribe<GameStateChanged>(OnGameStateChanged);
            DialogueSession.Changed -= RefreshDialogue;
            StationTeleport.MenuChanged -= RefreshStations;
            if (_kit != null)
                _kit.Changed -= RefreshKit;
        }

        private void Update()
        {
            RefreshPrompt();
            if (_toasts.RemoveAll(t => Time.unscaledTime >= t.expires) > 0)
                RefreshToasts();
            if (_vignetteAlpha > 0f)
                SetVignette(Mathf.MoveTowards(_vignetteAlpha, 0f, Time.unscaledDeltaTime * 0.25f));
        }

        private void RefreshPrompt()
        {
            bool playing = GameFlow.State == GameState.Playing;
            bool hasControl = CursorControl.HasControl;
            bool modal = DialogueSession.IsOpen || OverlayMenu.IsOpen;

            _hint.gameObject.SetActive(playing && !hasControl);
            _crosshair.enabled = playing && hasControl && !modal;

            var prompt = _interactor != null ? _interactor.Prompt : null;
            if (!playing || !hasControl || string.IsNullOrEmpty(prompt))
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
            string mode = SessionContext.IsGuided ? string.Empty : $"   <color={PendingColor}>· MODO EVALUACIÓN</color>";
            _stepTitle.text = $"<size=70%><color={GuideColor}>{number}</color>{mode}</size>\n{step.Title}";

            bool guided = SessionContext.IsGuided;
            _stepBody.gameObject.SetActive(guided);
            _taskList.gameObject.SetActive(guided);
            if (!guided)
                return;

            _stepBody.text = step.Description;
            _builder.Clear();
            for (int t = 0; t < step.Tasks.Count; t++)
            {
                bool done = progress.IsTaskDone(index, t);
                _builder.Append(done ? $"<color={DoneColor}>[x]  " : $"<color={PendingColor}>[  ]  ")
                    .Append(step.Tasks[t].Description)
                    .Append("</color>\n");
            }
            _taskList.text = _builder.ToString().TrimEnd('\n');
        }

        private void RefreshKit()
        {
            if (_kit == null || !_kit.IsCollected)
            {
                _kitBar.text = $"Kit LOTO: está en la estación de bloqueo     <color={PendingColor}>T: mapa de estaciones  ·  Esc: pausa</color>";
                return;
            }
            _kitBar.text =
                $"{Slot(1, LotoItemType.Padlock)}    {Slot(2, LotoItemType.Tag)}    {Slot(3, LotoItemType.ValveLockout)}" +
                $"    <color={PendingColor}>Llave: sí  ·  R: guardar  ·  T: mapa</color>";
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

        private void RefreshStations()
        {
            _stationPanel.SetActive(OverlayMenu.IsOpen);
            if (!OverlayMenu.IsOpen || _stations == null)
                return;
            _builder.Clear();
            _builder.Append($"<b><color={GuideColor}>Ir a…</color></b>\n\n");
            for (int i = 0; i < _stations.Stations.Count; i++)
                _builder.Append($"<b>[{i + 1}]</b>  {_stations.Stations[i].Name}\n");
            _builder.Append($"\n<size=80%><color={PendingColor}>T para cerrar</color></size>");
            _stationText.text = _builder.ToString();
        }

        private void OnStepStarted(StepStarted evt) => RefreshSteps();

        private void OnTaskCompleted(TaskCompleted evt) => RefreshSteps();

        private void OnToast(ToastRequested evt)
        {
            string color = evt.Kind switch
            {
                ToastKind.Success => DoneColor,
                ToastKind.Warning => GuideColor,
                ToastKind.Error => ErrorColor,
                _ => "#FFFFFF"
            };
            // Más tiempo para mensajes largos (explicaciones de errores). El más nuevo va arriba.
            float duration = Mathf.Clamp(2.5f + evt.Message.Length * 0.04f, 3f, 9f);
            _toasts.Insert(0, ($"<color={color}>{evt.Message}</color>", Time.unscaledTime + duration));
            if (_toasts.Count > MaxToasts)
                _toasts.RemoveAt(_toasts.Count - 1);
            RefreshToasts();
            if (evt.Kind == ToastKind.Error)
                SetVignette(0.2f);
        }

        private void RefreshToasts()
        {
            _toastPanel.SetActive(_toasts.Count > 0);
            _builder.Clear();
            for (int i = 0; i < _toasts.Count; i++)
            {
                if (i > 0)
                    _builder.Append("\n\n");
                _builder.Append(_toasts[i].text);
            }
            _toastText.text = _builder.ToString();
        }

        private void OnGameStateChanged(GameStateChanged evt)
        {
            _pausePanel.SetActive(evt.State == GameState.Paused);
            _stepPanel.SetActive(evt.State != GameState.Completed);
            _kitBar.transform.parent.gameObject.SetActive(evt.State is GameState.Playing or GameState.Paused);
            if (evt.State == GameState.Completed)
            {
                _toasts.Clear();
                RefreshToasts();
            }
            RefreshSteps();
        }

        private void SetVignette(float alpha)
        {
            _vignetteAlpha = alpha;
            var color = _vignette.color;
            color.a = alpha;
            _vignette.color = color;
            _vignette.enabled = alpha > 0.001f;
        }

        private void OnSensitivityChanged(float value)
        {
            Settings.LookSensitivity = value;
            if (_look != null)
                _look.ReloadSettings();
        }

        private void OnInvertChanged(bool value)
        {
            Settings.InvertY = value;
            if (_look != null)
                _look.ReloadSettings();
        }

        private static void Resume()
        {
            GameFlow.SetState(GameState.Playing);
            CursorControl.Lock();
        }
    }
}
