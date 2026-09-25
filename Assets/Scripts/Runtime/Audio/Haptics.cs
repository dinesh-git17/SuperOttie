#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace SuperOttie.Audio
{
    /// <summary>Light/medium/heavy taps on iPhone; a no-op everywhere else.</summary>
    public static class Haptics
    {
        public enum Strength
        {
            Light = 0,
            Medium = 1,
            Heavy = 2,
        }

        public static bool Enabled = true;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern void SuperOttie_Haptic(int style);

        public static void Play(Strength strength)
        {
            if (Enabled) SuperOttie_Haptic((int)strength);
        }
#else
        public static void Play(Strength strength) { }
#endif

        /// <summary>Which sound effects deserve a tap too.</summary>
        public static bool TryGetStrength(Sfx sfx, out Strength strength)
        {
            switch (sfx)
            {
                case Sfx.Bump:
                case Sfx.Kick:
                    strength = Strength.Light;
                    return true;
                case Sfx.Stomp:
                case Sfx.BrickBreak:
                case Sfx.PowerUp:
                    strength = Strength.Medium;
                    return true;
                case Sfx.Shrink:
                    strength = Strength.Heavy;
                    return true;
                default:
                    strength = Strength.Light;
                    return false;
            }
        }
    }
}
