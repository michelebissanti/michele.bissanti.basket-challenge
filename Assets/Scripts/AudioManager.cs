using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Audio;

/// <summary>
/// Centralized audio management system for handling sound effects and music.
/// Implements the Singleton pattern to ensure only one instance exists throughout the game.
/// </summary>
public class AudioManager : Singleton<AudioManager>
{
    #region Enums

    /// <summary>
    /// Enumeration of all available sound types in the game.
    /// Used to categorize and reference specific audio clips.
    /// </summary>
    public enum SoundType
    {
        Throw,
        Score,
        PerfectScore,
        OnBackboardBonusActivated,
        MenuMusic,
        GameMusic,
        OnFireballModeActivated,
        BackboardScore
    }

    #endregion

    #region Nested Classes

    /// <summary>
    /// Serializable class representing a sound configuration.
    /// Contains all necessary properties to play an audio clip with specific settings.
    /// </summary>
    [System.Serializable]
    public class Sound
    {
        /// <summary>The type identifier for this sound.</summary>
        public SoundType Type;

        /// <summary>The audio clip to be played.</summary>
        public AudioClip Clip;

        /// <summary>The volume level for this sound (0 = silent, 1 = full volume).</summary>
        [Range(0f, 1f)]
        public float Volume = 1f;

        /// <summary>The AudioSource component associated with this sound at runtime (hidden in Inspector).</summary>
        [HideInInspector]
        public AudioSource Source;
    }

    #endregion

    #region Serialized Fields

    /// <summary>Array of all sounds configured in the Inspector.</summary>
    [SerializeField] private Sound[] AllSounds;

    /// <summary>Audio mixer group for music tracks, allowing independent volume control.</summary>
    [SerializeField] private AudioMixerGroup _musicMixerGroup;

    /// <summary>Audio mixer group for sound effects, allowing independent volume control.</summary>
    [SerializeField] private AudioMixerGroup _soundMixerGroup;

    #endregion

    #region Private Fields

    /// <summary>Dictionary for fast runtime lookup of sounds by their type.</summary>
    private Dictionary<SoundType, Sound> _soundDictionary = new Dictionary<SoundType, Sound>();

    /// <summary>Dedicated AudioSource for continuous music playback.</summary>
    private AudioSource _musicSource;

    #endregion

    #region Unity Lifecycle Methods

    /// <summary>
    /// Initializes the AudioManager by populating the sound dictionary.
    /// Called automatically when the GameObject is instantiated.
    /// </summary>
    public override void Awake()
    {
        base.Awake();

        // Populate the dictionary for efficient sound lookup at runtime
        foreach (var s in AllSounds)
        {
            _soundDictionary[s.Type] = s;
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Plays a one-shot sound effect of the specified type.
    /// Creates a temporary AudioSource that is automatically destroyed after playback.
    /// </summary>
    /// <param name="type">The type of sound to play.</param>
    public void Play(SoundType type)
    {
        // Validate that the requested sound type exists in the dictionary
        if (!_soundDictionary.TryGetValue(type, out Sound s))
        {
            Debug.LogWarning($"Sound type {type} not found!");
            return;
        }

        // Create a temporary GameObject to host the AudioSource
        var soundObj = new GameObject($"Sound_{type}");
        var audioSrc = soundObj.AddComponent<AudioSource>();

        // Configure the AudioSource with the sound's properties
        audioSrc.clip = s.Clip;
        audioSrc.volume = s.Volume;
        audioSrc.outputAudioMixerGroup = _soundMixerGroup;

        // Play the audio clip
        audioSrc.Play();

        // Schedule destruction of the GameObject after the clip finishes playing
        Destroy(soundObj, s.Clip.length);
    }

    /// <summary>
    /// Changes the currently playing background music to a new track.
    /// If no music is currently playing, initializes the music source.
    /// </summary>
    /// <param name="type">The type of music track to play.</param>
    public void ChangeMusic(SoundType type)
    {
        // Validate that the requested music track exists in the dictionary
        if (!_soundDictionary.TryGetValue(type, out Sound track))
        {
            Debug.LogWarning($"Music track {type} not found!");
            return;
        }

        // Initialize the music source if it hasn't been created yet
        if (_musicSource == null)
        {
            var container = new GameObject("SoundTrackObj");
            _musicSource = container.AddComponent<AudioSource>();
            _musicSource.loop = true; // Enable looping for continuous background music
            _musicSource.outputAudioMixerGroup = _musicMixerGroup;
        }

        // Set the new music track and begin playback
        _musicSource.clip = track.Clip;
        _musicSource.Play();
    }

    #endregion
}