using System;
using System.Collections.Generic;
using UnityEngine;

namespace LotoSim.Audio
{
    public enum SoundId
    {
        SwitchLever = 0,
        ValveTurn = 1,
        PadlockClose = 2,
        PadlockOpen = 3,
        TagAttach = 4,
        LockoutAttach = 5,
        Button = 6,
        PickUp = 7,
        Footstep = 8,
        Spark = 9,
        UiClick = 20,
        UiConfirm = 21,
        UiStepComplete = 22,
        UiError = 23,
        UiWarning = 24,
        UiOpen = 25,
        UiClose = 26,
        UiTick = 27,
        UiReport = 28
    }

    /// <summary>Catálogo de sonidos: cada id tiene variantes, volumen y variación de tono.</summary>
    [CreateAssetMenu(menuName = "LotoSim/Sound Library", fileName = "SND_Library")]
    public sealed class SoundLibrary : ScriptableObject
    {
        [SerializeField]
        private List<SoundEntry> _entries = new();

        public bool TryGet(SoundId id, out SoundEntry entry)
        {
            foreach (var e in _entries)
            {
                if (e.Id == id && e.Clips.Length > 0)
                {
                    entry = e;
                    return true;
                }
            }
            entry = null;
            return false;
        }

#if UNITY_EDITOR
        public void EditorInitialize(List<SoundEntry> entries) => _entries = entries;
#endif
    }

    [Serializable]
    public sealed class SoundEntry
    {
        [SerializeField]
        private SoundId _id;

        [SerializeField]
        private AudioClip[] _clips;

        [SerializeField, Range(0f, 1f)]
        private float _volume = 1f;

        [SerializeField, Range(0f, 0.5f)]
        private float _pitchVariance = 0.05f;

        public SoundEntry(SoundId id, AudioClip[] clips, float volume, float pitchVariance)
        {
            _id = id;
            _clips = clips;
            _volume = volume;
            _pitchVariance = pitchVariance;
        }

        public SoundId Id => _id;
        public AudioClip[] Clips => _clips;
        public float Volume => _volume;
        public float PitchVariance => _pitchVariance;

        public AudioClip PickClip() => _clips[UnityEngine.Random.Range(0, _clips.Length)];
    }
}
