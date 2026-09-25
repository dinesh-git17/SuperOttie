using System;
using SuperOttie.Audio;
using SuperOttie.Core;
using SuperOttie.Game;
using UnityEngine;

namespace SuperOttie.Level
{
    /// <summary>
    /// Services and events shared by everything inside one running level. Passed to entities when
    /// they are spawned instead of using global singletons, so levels can be built in isolation (tests).
    /// </summary>
    public sealed class LevelContext
    {
        public LevelContext(LevelData data, GameAssets assets, GameSession session, IGameAudio audio, Camera camera)
        {
            Data = data;
            Assets = assets;
            Session = session;
            Audio = audio ?? new NullAudio();
            Camera = camera;
        }

        public LevelData Data { get; }
        public GameAssets Assets { get; }
        public GameSession Session { get; }
        public IGameAudio Audio { get; }
        public Camera Camera { get; }
        public Transform Root { get; set; }

        /// <summary>Anything that falls below this height is gone (pits).</summary>
        public float KillY => -3f;

        public event Action PlayerDied;
        public event Action<int> GoalReached;
        public event Action PlayerFinishedGoal;

        public void RaisePlayerDied() => PlayerDied?.Invoke();
        public void RaiseGoalReached(int flagScore) => GoalReached?.Invoke(flagScore);
        public void RaisePlayerFinishedGoal() => PlayerFinishedGoal?.Invoke();

        /// <summary>World-space right edge of what the camera shows (enemies wake up just beyond it).</summary>
        public float CameraRightEdge
        {
            get
            {
                if (Camera == null) return float.PositiveInfinity;
                return Camera.transform.position.x + Camera.orthographicSize * Camera.aspect;
            }
        }

        public float CameraLeftEdge
        {
            get
            {
                if (Camera == null) return float.NegativeInfinity;
                return Camera.transform.position.x - Camera.orthographicSize * Camera.aspect;
            }
        }
    }
}
