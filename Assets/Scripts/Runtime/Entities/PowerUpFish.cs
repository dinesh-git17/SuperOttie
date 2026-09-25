using System.Collections;
using SuperOttie.Core;
using SuperOttie.Level;
using SuperOttie.Player;
using UnityEngine;

namespace SuperOttie.Entities
{
    /// <summary>The golden fish (this game's mushroom): rises out of a block, then scoots along the ground.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class PowerUpFish : MonoBehaviour, ICollectible
    {
        public const float Speed = 2.8f;
        const float EmergeTime = 0.7f;

        LevelContext _ctx;
        Rigidbody2D _rb;
        BoxCollider2D _box;
        SpriteRenderer _sr;
        int _dir = 1;
        bool _moving;
        bool _collected;

        public bool IsMoving => _moving;

        public static PowerUpFish Spawn(LevelContext ctx, Vector3 blockCenter, Transform parent)
        {
            var go = new GameObject("PowerUpFish") { layer = Layers.Item };
            go.transform.SetParent(parent, false);
            go.transform.position = blockCenter;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.8f, 0.6f);
            box.sharedMaterial = SpriteObjects.Frictionless;
            var sr = SpriteObjects.Create("Visual", ctx.Assets.fish, go.transform, Vector3.zero, Sorting.EmergingItem);
            var fish = go.AddComponent<PowerUpFish>();
            fish._ctx = ctx;
            fish._sr = sr;
            return fish;
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _box = GetComponent<BoxCollider2D>();
        }

        IEnumerator Start()
        {
            var from = transform.position;
            var to = from + Vector3.up * 0.8f;
            for (float t = 0f; t < EmergeTime; t += Time.deltaTime)
            {
                if (_collected) yield break;
                transform.position = Vector3.Lerp(from, to, t / EmergeTime);
                yield return null;
            }
            transform.position = to;
            _sr.sortingOrder = Sorting.Item;
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 4f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _moving = true;
        }

        void FixedUpdate()
        {
            if (!_moving) return;
            var b = _box.bounds;
            var hit = Physics2D.BoxCast(b.center, new Vector2(0.05f, b.size.y * 0.6f), 0f, Vector2.right * _dir, b.extents.x + 0.04f, Layers.GroundMask);
            if (hit.collider != null && Mathf.Abs(hit.normal.x) > 0.7f) _dir = -_dir;
            _rb.linearVelocity = new Vector2(_dir * Speed, _rb.linearVelocity.y);
            _sr.flipX = _dir < 0;
            if (transform.position.y < _ctx.KillY) Destroy(gameObject);
        }

        /// <summary>Bumped from below by a block: hop and reverse.</summary>
        public void Hop()
        {
            if (!_moving) return;
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 9f);
        }

        public void Collect(PlayerController player)
        {
            if (_collected) return;
            _collected = true;
            player.CollectPowerUp();
            Destroy(gameObject);
        }
    }
}
