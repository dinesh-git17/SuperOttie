using SuperOttie.Audio;
using SuperOttie.Core;
using SuperOttie.Level;
using UnityEngine;

namespace SuperOttie.Entities
{
    public abstract class Enemy : MonoBehaviour
    {
        /// <summary>How far beyond the right edge of the screen enemies wake up.</summary>
        public const float WakeMargin = 1.5f;

        protected LevelContext Ctx;
        protected SpriteRenderer Visual;

        public bool IsDead { get; protected set; }
        public bool IsAwake { get; private set; }
        public virtual bool CanBeStomped => true;
        public Collider2D Hitbox { get; protected set; }

        /// <summary>Height above which the player's feet count as "landing on top" of this enemy.</summary>
        public float StompLineY => Hitbox.bounds.center.y;

        protected void BaseInit(LevelContext ctx, SpriteRenderer visual, Collider2D hitbox)
        {
            Ctx = ctx;
            Visual = visual;
            Hitbox = hitbox;
        }

        /// <summary>Enemies sleep until the camera is about to show them, like the classics.</summary>
        protected bool UpdateWake()
        {
            if (IsAwake) return true;
            if (transform.position.x - 0.5f > Ctx.CameraRightEdge + WakeMargin) return false;
            IsAwake = true;
            OnWake();
            return true;
        }

        protected virtual void OnWake() { }

        public abstract void Stomp();

        /// <summary>Defeated by a hit from below (bumped block) - flips over and falls off screen.</summary>
        public virtual void KnockOut(int direction)
        {
            if (IsDead) return;
            IsDead = true;
            Hitbox.enabled = false;
            if (TryGetComponent<Rigidbody2D>(out var rb)) rb.simulated = false;
            Visual.flipY = true;
            Visual.sortingOrder = Sorting.Effects;
            var b = gameObject.AddComponent<Ballistic>();
            b.Velocity = new Vector2(direction * 2f, 9f);
            b.KillY = Ctx.KillY;
            Ctx.Session.AddScore(100);
            Ctx.Audio.Play(Sfx.Kick);
            Ctx.RaisePopup(transform.position + Vector3.up, "100");
        }
    }

    /// <summary>Grumpy crab: walks, turns at walls, walks off ledges, can be stomped flat.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class CrabEnemy : Enemy
    {
        public const float Speed = 1.6f;
        const float FrameRate = 6f;

        Rigidbody2D _rb;
        BoxCollider2D _box;
        int _dir = -1;
        float _anim;

        public int Direction => _dir;

        public void Init(LevelContext ctx, SpriteRenderer visual)
        {
            _rb = GetComponent<Rigidbody2D>();
            _box = GetComponent<BoxCollider2D>();
            BaseInit(ctx, visual, _box);
            _rb.simulated = false;
        }

        protected override void OnWake() => _rb.simulated = true;

        void FixedUpdate()
        {
            if (IsDead || !UpdateWake()) return;
            var b = _box.bounds;
            var hit = Physics2D.BoxCast(b.center, new Vector2(0.05f, b.size.y * 0.6f), 0f, Vector2.right * _dir, b.extents.x + 0.04f, Layers.GroundMask);
            if (hit.collider != null && Mathf.Abs(hit.normal.x) > 0.7f) _dir = -_dir;
            _rb.linearVelocity = new Vector2(_dir * Speed, _rb.linearVelocity.y);

            if (transform.position.y < Ctx.KillY || transform.position.x < Ctx.CameraLeftEdge - 14f) Destroy(gameObject);
        }

        void Update()
        {
            if (IsDead || !IsAwake) return;
            var frames = Ctx.Assets.crabWalk;
            if (frames.Length > 0)
            {
                _anim += Time.deltaTime * FrameRate;
                Visual.sprite = frames[(int)_anim % frames.Length];
            }
            Visual.flipX = _dir > 0; // art faces left
        }

        public override void Stomp()
        {
            if (IsDead) return;
            IsDead = true;
            _box.enabled = false;
            _rb.simulated = false;
            if (Ctx.Assets.crabFlat != null) Visual.sprite = Ctx.Assets.crabFlat;
            Destroy(gameObject, 0.5f);
        }
    }

    /// <summary>Spiky pufferfish: bobs up and down in place. Too spiky to stomp.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class PufferEnemy : Enemy
    {
        public const float Amplitude = 1.6f;
        public const float Period = 2.6f;

        Rigidbody2D _rb;
        Vector2 _origin;
        float _t;
        float _anim;

        public override bool CanBeStomped => false;

        public void Init(LevelContext ctx, SpriteRenderer visual)
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            BaseInit(ctx, visual, GetComponent<CircleCollider2D>());
            _origin = transform.position;
            _t = _origin.x * 0.37f; // desynchronise neighbours
        }

        void FixedUpdate()
        {
            if (IsDead || !UpdateWake()) return;
            _t += Time.fixedDeltaTime;
            float y = Mathf.Sin(_t * Mathf.PI * 2f / Period) * Amplitude;
            _rb.MovePosition(_origin + new Vector2(0f, y));
        }

        void Update()
        {
            if (IsDead) return;
            var frames = Ctx.Assets.puffer;
            if (frames.Length > 0)
            {
                _anim += Time.deltaTime * 4f;
                Visual.sprite = frames[(int)_anim % frames.Length];
            }
            float pulse = 1f + Mathf.Sin(Time.time * 5f + _origin.x) * 0.04f;
            Visual.transform.localScale = new Vector3(pulse, 2f - pulse, 1f);
        }

        public override void Stomp() { } // cannot be stomped; the player gets hurt instead
    }
}
