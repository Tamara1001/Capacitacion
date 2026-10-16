using UnityEngine;
using UnityEngine.SceneManagement;

namespace LotoSim.Core
{
    /// <summary>Estado global de la partida (jugando, pausa, completada).</summary>
    public static class GameFlow
    {
        public static GameState State { get; private set; } = GameState.Playing;

        public static void SetState(GameState state)
        {
            if (State == state)
                return;
            State = state;
            Time.timeScale = state == GameState.Paused ? 0f : 1f;
            SimEvents.Publish(new GameStateChanged(state));
        }

        public static void TogglePause()
        {
            if (State == GameState.Playing)
                SetState(GameState.Paused);
            else if (State == GameState.Paused)
                SetState(GameState.Playing);
        }

        public const string MenuScene = "Menu";
        public const string TrainingScene = "PumpRoom";

        public static void RestartScene() => LoadScene(SceneManager.GetActiveScene().name);

        public static void LoadScene(string sceneName)
        {
            State = GameState.Playing;
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            State = GameState.Playing;
            Time.timeScale = 1f;
        }
    }
}
