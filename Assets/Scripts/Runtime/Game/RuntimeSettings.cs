using SuperOttie.Core;
using UnityEngine;

namespace SuperOttie.Game
{
    public static class RuntimeSettings
    {
        public const float FixedTimestep = 1f / 60f;

        /// <summary>Global engine settings the game relies on. Safe to call more than once (tests do).</summary>
        public static void Apply()
        {
            Time.fixedDeltaTime = FixedTimestep;
            Layers.ConfigureCollisionMatrix();
            Physics2D.queriesHitTriggers = true;
            Physics2D.queriesStartInColliders = true;

            // ProMotion iPhones run at 120 Hz; the physics step is interpolated so any rate looks smooth.
            int refresh = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
            Application.targetFrameRate = refresh >= 90 ? 120 : 60;

            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
        }
    }
}
