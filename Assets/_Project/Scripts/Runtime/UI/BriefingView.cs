using LotoSim.Core;
using LotoSim.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LotoSim.UI
{
    /// <summary>Pantalla inicial dentro de la sala: qué es LOTO, la tarea y los controles.</summary>
    [DisallowMultipleComponent]
    public sealed class BriefingView : MonoBehaviour
    {
        [SerializeField]
        private GameObject _panel;

        [SerializeField]
        private TMP_Text _modeLabel;

        [SerializeField]
        private Button _startButton;

        private void Awake() => _startButton.onClick.AddListener(Begin);

        private void OnEnable() => SimEvents.Subscribe<GameStateChanged>(OnGameStateChanged);

        private void OnDisable() => SimEvents.Unsubscribe<GameStateChanged>(OnGameStateChanged);

        private void Start() => Refresh(GameFlow.State);

        private void OnGameStateChanged(GameStateChanged evt) => Refresh(evt.State);

        private void Refresh(GameState state)
        {
            _panel.SetActive(state == GameState.Briefing);
            _modeLabel.text = SessionContext.IsGuided
                ? $"<color={HudView.GuideColor}>MODO GUIADO</color>  ·  Te vamos a indicar cada paso y explicar los errores."
                : $"<color={HudView.ErrorColor}>MODO EVALUACIÓN</color>  ·  Sin ayudas: los errores se informan al final.";
        }

        private static void Begin()
        {
            GameFlow.SetState(GameState.Playing);
            CursorControl.Lock();
        }
    }
}
