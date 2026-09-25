using UnityEngine;

namespace SuperOttie.Audio
{
    public enum Sfx
    {
        Jump,
        JumpBig,
        Coin,
        Stomp,
        Bump,
        BrickBreak,
        PowerUpAppear,
        PowerUp,
        Shrink,
        OneUp,
        Kick,
        Flagpole,
        Pause,
        Hurry,
        UiTap,
        Fireworks,
    }

    public interface IGameAudio
    {
        void Play(Sfx sfx);
        void PlayMusic(AudioClip clip, bool loop = true, float pitch = 1f);
        void StopMusic();
    }

    /// <summary>Silent implementation for tests and headless runs.</summary>
    public sealed class NullAudio : IGameAudio
    {
        public void Play(Sfx sfx) { }
        public void PlayMusic(AudioClip clip, bool loop = true, float pitch = 1f) { }
        public void StopMusic() { }
    }
}
