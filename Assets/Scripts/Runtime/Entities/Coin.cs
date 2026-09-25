using SuperOttie.Audio;
using SuperOttie.Level;
using SuperOttie.Player;
using UnityEngine;

namespace SuperOttie.Entities
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class Coin : MonoBehaviour, ICollectible
    {
        LevelContext _ctx;
        Transform _visual;
        float _phase;
        bool _collected;

        public void Init(LevelContext ctx, Transform visual)
        {
            _ctx = ctx;
            _visual = visual;
            _phase = transform.position.x * 0.7f; // neighbours shimmer out of step
        }

        public void Collect(PlayerController player)
        {
            if (_collected) return;
            _collected = true;
            _ctx.Session.AddCoin();
            _ctx.Audio.Play(Sfx.Coin);
            Destroy(gameObject);
        }

        void Update()
        {
            if (_visual == null) return;
            _phase += Time.deltaTime;
            float spin = Mathf.Abs(Mathf.Cos(_phase * 2.2f));
            _visual.localScale = new Vector3(0.25f + 0.75f * spin, 1f, 1f);
            _visual.localPosition = new Vector3(0f, Mathf.Sin(_phase * 3f) * 0.05f, 0f);
        }
    }
}
