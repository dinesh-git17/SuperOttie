using System.Collections;
using SuperOttie.Level;
using SuperOttie.Player;
using UnityEngine;

namespace SuperOttie.Entities
{
    /// <summary>End-of-level flagpole. Grabbing it higher up is worth more points.</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class GoalPole : MonoBehaviour
    {
        public const float PoleHeight = 8f;
        public const float SlideSpeed = 9f;

        LevelContext _ctx;
        Transform _flag;
        bool _reached;

        /// <summary>World Y of the top of the stone base block, where the slide ends.</summary>
        public float BaseY => transform.position.y;
        public float X => transform.position.x;
        public bool IsReached => _reached;

        public void Init(LevelContext ctx, Transform flag)
        {
            _ctx = ctx;
            _flag = flag;
        }

        public static int ScoreForHeight(float heightAboveBase)
        {
            if (heightAboveBase >= 7f) return 5000;
            if (heightAboveBase >= 5f) return 2000;
            if (heightAboveBase >= 3.5f) return 800;
            if (heightAboveBase >= 2f) return 400;
            return 100;
        }

        public void Reach(PlayerController player)
        {
            if (_reached) return;
            _reached = true;
            int score = ScoreForHeight(player.transform.position.y - BaseY);
            _ctx.Session.AddScore(score);
            _ctx.RaisePopup(player.transform.position + Vector3.up * 1.5f, score.ToString());
            _ctx.RaiseGoalReached(score);
            player.BeginGoal(this);
            StartCoroutine(LowerFlag());
        }

        IEnumerator LowerFlag()
        {
            if (_flag == null) yield break;
            float bottom = BaseY + 0.9f;
            while (_flag.position.y > bottom)
            {
                _flag.position += Vector3.down * (SlideSpeed * Time.deltaTime);
                yield return null;
            }
            _flag.position = new Vector3(_flag.position.x, bottom, _flag.position.z);
        }
    }
}
