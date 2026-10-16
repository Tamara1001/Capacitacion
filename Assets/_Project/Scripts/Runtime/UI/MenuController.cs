using LotoSim.Core;
using LotoSim.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LotoSim.UI
{
    /// <summary>Menú principal: nombre del participante, modo de capacitación, opciones y créditos.</summary>
    [DisallowMultipleComponent]
    public sealed class MenuController : MonoBehaviour
    {
        [SerializeField]
        private TMP_InputField _nameInput;

        [SerializeField]
        private Button _guidedButton;

        [SerializeField]
        private Button _assessmentButton;

        [SerializeField]
        private Button _optionsButton;

        [SerializeField]
        private Button _creditsButton;

        [SerializeField]
        private GameObject _optionsPanel;

        [SerializeField]
        private Slider _sensitivitySlider;

        [SerializeField]
        private Toggle _invertToggle;

        [SerializeField]
        private Button _optionsCloseButton;

        [SerializeField]
        private GameObject _creditsPanel;

        [SerializeField]
        private Button _creditsCloseButton;

        private void Awake()
        {
            CursorControl.Unlock();
            Time.timeScale = 1f;

            _nameInput.text = SessionContext.TraineeName == "Técnico" ? string.Empty : SessionContext.TraineeName;
            _guidedButton.onClick.AddListener(() => StartTraining(GameMode.Guided));
            _assessmentButton.onClick.AddListener(() => StartTraining(GameMode.Assessment));
            _optionsButton.onClick.AddListener(() => _optionsPanel.SetActive(true));
            _optionsCloseButton.onClick.AddListener(() => _optionsPanel.SetActive(false));
            _creditsButton.onClick.AddListener(() => _creditsPanel.SetActive(true));
            _creditsCloseButton.onClick.AddListener(() => _creditsPanel.SetActive(false));

            _sensitivitySlider.minValue = 0.02f;
            _sensitivitySlider.maxValue = 0.5f;
            _sensitivitySlider.SetValueWithoutNotify(Settings.LookSensitivity);
            _sensitivitySlider.onValueChanged.AddListener(v => Settings.LookSensitivity = v);
            _invertToggle.SetIsOnWithoutNotify(Settings.InvertY);
            _invertToggle.onValueChanged.AddListener(v => Settings.InvertY = v);

            _optionsPanel.SetActive(false);
            _creditsPanel.SetActive(false);
        }

        private void StartTraining(GameMode mode)
        {
            var trimmed = _nameInput.text.Trim();
            SessionContext.TraineeName = string.IsNullOrEmpty(trimmed) ? "Técnico" : trimmed;
            SessionContext.Mode = mode;
            GameFlow.LoadScene(GameFlow.TrainingScene);
        }
    }
}
