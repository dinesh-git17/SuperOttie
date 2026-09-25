using System.Collections.Generic;
using SuperOttie.Game;
using UnityEngine;

namespace SuperOttie.Audio
{
    /// <summary>Music (one looping source) plus a small pool of one-shot sources for effects.</summary>
    public sealed class AudioManager : MonoBehaviour, IGameAudio
    {
        const int PoolSize = 8;
        const string MutedKey = "superottie.muted";

        [SerializeField] float musicVolume = 0.55f;
        [SerializeField] float sfxVolume = 0.85f;

        readonly Dictionary<Sfx, SfxClip> _clips = new Dictionary<Sfx, SfxClip>();
        readonly Dictionary<Sfx, float> _lastPlayed = new Dictionary<Sfx, float>();
        AudioSource _music;
        AudioSource[] _pool;
        int _next;
        bool _muted;

        public bool Muted
        {
            get => _muted;
            set
            {
                _muted = value;
                AudioListener.volume = value ? 0f : 1f;
                PlayerPrefs.SetInt(MutedKey, value ? 1 : 0);
            }
        }

        public void Init(GameAssets assets)
        {
            foreach (var s in assets.sfx)
                if (s.clip != null) _clips[s.id] = s;

            _music = gameObject.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
            _music.volume = musicVolume;

            _pool = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _pool[i] = src;
            }
            Muted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
        }

        public void Play(Sfx sfx)
        {
            if (_pool == null || !_clips.TryGetValue(sfx, out var entry)) return;
            // Identical sounds fired in the same instant just get louder and clip; drop the duplicates.
            float now = Time.unscaledTime;
            if (_lastPlayed.TryGetValue(sfx, out var last) && now - last < 0.035f) return;
            _lastPlayed[sfx] = now;

            var src = _pool[_next];
            _next = (_next + 1) % _pool.Length;
            src.pitch = 1f;
            src.PlayOneShot(entry.clip, entry.volume * sfxVolume);
        }

        public void PlayMusic(AudioClip clip, bool loop = true, float pitch = 1f)
        {
            if (_music == null) return;
            if (clip == null)
            {
                _music.Stop();
                return;
            }
            if (_music.clip == clip && _music.isPlaying && _music.loop == loop && Mathf.Approximately(_music.pitch, pitch)) return;
            _music.clip = clip;
            _music.loop = loop;
            _music.pitch = pitch;
            _music.Play();
        }

        public void StopMusic()
        {
            if (_music != null) _music.Stop();
        }

        public void SetMusicPaused(bool paused)
        {
            if (_music == null) return;
            if (paused) _music.Pause();
            else _music.UnPause();
        }

        public bool IsMusicPlaying => _music != null && _music.isPlaying;
    }
}
