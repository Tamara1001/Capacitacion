using LotoSim.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LotoSim.Audio
{
    /// <summary>Click en todos los botones y toggles del canvas (también en el menú, sin AudioService).</summary>
    [DisallowMultipleComponent]
    public sealed class UiSounds : MonoBehaviour
    {
        [SerializeField]
        private AudioClip _click;

        [SerializeField, Range(0f, 1f)]
        private float _volume = 0.6f;

        private AudioSource _source;

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.ignoreListenerPause = true;
            AudioListener.volume = Settings.MasterVolume;

            foreach (var button in GetComponentsInChildren<Button>(true))
                button.onClick.AddListener(Click);
            foreach (var toggle in GetComponentsInChildren<Toggle>(true))
                toggle.onValueChanged.AddListener(_ => Click());
        }

        private void Click()
        {
            if (_click != null)
                _source.PlayOneShot(_click, _volume);
        }
    }
}
