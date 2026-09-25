using System.Collections;
using SuperOttie.Audio;
using SuperOttie.Level;
using SuperOttie.Player;
using UnityEngine;

namespace SuperOttie.Entities
{
    /// <summary>Mid-level signpost: once Ottie passes it, dying restarts here instead of at the beginning.</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class Checkpoint : MonoBehaviour, ICollectible
    {
        LevelContext _ctx;
        SpriteRenderer _visual;

        public bool IsReached { get; private set; }

        public void Init(LevelContext ctx, SpriteRenderer visual, bool startReached)
        {
            _ctx = ctx;
            _visual = visual;
            IsReached = startReached;
            if (startReached) _visual.color = new Color(1f, 0.95f, 0.6f);
        }

        public void Collect(PlayerController player)
        {
            if (IsReached) return;
            IsReached = true;
            _ctx.Audio.Play(Sfx.OneUp);
            _ctx.RaiseCheckpointReached();
            StartCoroutine(Celebrate());
        }

        IEnumerator Celebrate()
        {
            var t0 = _visual.transform.localScale;
            for (float t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                float s = 1f + Mathf.Sin(t / 0.5f * Mathf.PI) * 0.35f;
                _visual.transform.localScale = new Vector3(t0.x * s, t0.y * s, 1f);
                _visual.color = Color.Lerp(Color.white, new Color(1f, 0.95f, 0.6f), t / 0.5f);
                yield return null;
            }
            _visual.transform.localScale = t0;
        }
    }
}
