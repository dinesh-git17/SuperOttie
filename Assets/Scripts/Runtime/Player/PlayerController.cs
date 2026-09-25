using System.Collections;
using System.Collections.Generic;
using SuperOttie.Audio;
using SuperOttie.Core;
using SuperOttie.Entities;
using SuperOttie.Input;
using SuperOttie.Level;
using UnityEngine;

namespace SuperOttie.Player
{
    /// <summary>
    /// Ottie. Movement comes from <see cref="PlatformerMotor"/>; this component bridges it to Unity
    /// physics and resolves interactions (stomps, pickups, block bumps, the goal) with explicit
    /// overlap queries rather than collision callbacks, so the outcome never depends on callback order.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        public enum LifeState
        {
            Alive,
            Dying,
            Goal,
        }

        public const float BigScale = 1.4f;
        public const float HurtInvincibility = 2f;
        public static readonly Vector2 ColliderSize = new Vector2(0.58f, 0.86f);
        const float EdgeRadius = 0.04f;

        [SerializeField] MotorSettings motorSettings = new MotorSettings();

        readonly List<Collider2D> _overlaps = new List<Collider2D>(8);
        readonly RaycastHit2D[] _hits = new RaycastHit2D[6];
        readonly StompChain _stompChain = new StompChain();

        LevelContext _ctx;
        IPlayerInput _input;
        PlayerVisual _visual;
        Rigidbody2D _rb;
        BoxCollider2D _box;
        InputFrame _frame;
        float _lastCommandedVy;
        bool _wasGrounded;

        public PlatformerMotor Motor { get; private set; }
        public LifeState State { get; private set; } = LifeState.Alive;
        public bool IsBig { get; private set; }
        public bool IsGrounded { get; private set; }
        public int Facing { get; private set; } = 1;
        public float InvincibleTime { get; private set; }
        public bool IsInvincible => InvincibleTime > 0f;
        public Vector2 Velocity => Motor.Velocity;
        public bool FinishedGoal { get; private set; }

        public static PlayerController Spawn(LevelContext ctx, IPlayerInput input, Vector2 feetPosition, Transform parent)
        {
            var go = new GameObject("Ottie") { layer = Layers.Player };
            go.transform.SetParent(parent, false);
            go.transform.position = feetPosition;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f; // the motor owns gravity
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

            var box = go.AddComponent<BoxCollider2D>();
            box.size = ColliderSize - Vector2.one * (EdgeRadius * 2f);
            box.edgeRadius = EdgeRadius;
            box.offset = new Vector2(0f, ColliderSize.y * 0.5f);
            box.sharedMaterial = SpriteObjects.Frictionless;

            var sr = SpriteObjects.Create("Visual", ctx.Assets.playerIdle, go.transform, Vector3.zero, Sorting.Player);
            var visual = go.AddComponent<PlayerVisual>();
            visual.Init(sr, ctx.Assets);

            var player = go.AddComponent<PlayerController>();
            player._ctx = ctx;
            player._input = input;
            player._visual = visual;
            return player;
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _box = GetComponent<BoxCollider2D>();
            Motor = new PlatformerMotor(motorSettings);
        }

        public void SetInput(IPlayerInput input) => _input = input;

        void Update()
        {
            if (State == LifeState.Goal) _visual.Tick(this, Time.deltaTime);
            if (State != LifeState.Alive) return;
            _frame = _input != null ? _input.Poll() : default;
            if (_frame.JumpPressed) Motor.QueueJump();
            if (Mathf.Abs(_frame.Move) > 0.1f) Facing = _frame.Move > 0f ? 1 : -1;
            if (InvincibleTime > 0f) InvincibleTime -= Time.deltaTime;
            _visual.Tick(this, Time.deltaTime);
        }

        void FixedUpdate()
        {
            if (State != LifeState.Alive) return;
            float dt = Time.fixedDeltaTime;
            Vector2 v = _rb.linearVelocity;

            // The engine zeroes velocity on contact; a sudden loss of upward speed means the head hit something.
            if (_lastCommandedVy > 0.5f && v.y < _lastCommandedVy * 0.5f) HitCeiling();

            Motor.Velocity = new Vector2(v.x, Mathf.Min(v.y, Motor.Velocity.y));
            IsGrounded = Motor.Velocity.y <= 0.01f && CheckGround();
            if (IsGrounded)
            {
                _stompChain.Reset();
                if (!_wasGrounded) _visual.OnLand();
            }
            _wasGrounded = IsGrounded;

            var step = Motor.Step(_frame.Move, _frame.JumpHeld, IsGrounded, dt);
            if (step.Jumped)
            {
                _ctx.Audio.Play(IsBig ? Sfx.JumpBig : Sfx.Jump);
                _visual.OnJump();
                IsGrounded = false;
            }
            _rb.linearVelocity = Motor.Velocity;
            _lastCommandedVy = Motor.Velocity.y;

            ResolveOverlaps();
            if (State == LifeState.Alive && transform.position.y < _ctx.KillY) Die();
        }

        bool CheckGround()
        {
            var b = _box.bounds;
            var origin = new Vector2(b.center.x, b.min.y + 0.05f);
            var size = new Vector2(b.size.x * 0.9f, 0.02f);
            var hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down, 0.1f, Layers.GroundMask);
            return hit.collider != null && hit.normal.y > 0.6f;
        }

        void HitCeiling()
        {
            Motor.HitCeiling();
            var b = _box.bounds;
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = Layers.GroundMask };
            int n = Physics2D.BoxCast(new Vector2(b.center.x, b.max.y - 0.05f), new Vector2(b.size.x * 0.85f, 0.02f), 0f, Vector2.up, filter, _hits, 0.2f);

            // Hit the one block nearest to Ottie's centre, like the classics.
            IBumpable best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var bumpable = _hits[i].collider.GetComponent<IBumpable>();
                if (bumpable == null) continue;
                float d = Mathf.Abs(_hits[i].collider.transform.position.x - b.center.x);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = bumpable;
                }
            }
            if (best != null) best.Bump(this);
            else if (n > 0) _ctx.Audio.Play(Sfx.Bump);
        }

        void ResolveOverlaps()
        {
            var b = _box.bounds;
            var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = Layers.EnemyMask | Layers.ItemMask };
            _overlaps.Clear();
            Physics2D.OverlapBox(b.center, (Vector2)b.size + new Vector2(0.04f, 0.04f), 0f, filter, _overlaps);
            foreach (var c in _overlaps)
            {
                if (State != LifeState.Alive) return;
                if (c == null || !c.enabled) continue;
                if (c.TryGetComponent<ICollectible>(out var item))
                {
                    item.Collect(this);
                    continue;
                }
                if (c.TryGetComponent<GoalPole>(out var goal))
                {
                    goal.Reach(this);
                    return;
                }
                var enemy = c.GetComponentInParent<Enemy>();
                if (enemy != null && !enemy.IsDead) ResolveEnemy(enemy, b);
            }
        }

        void ResolveEnemy(Enemy enemy, Bounds me)
        {
            bool descending = Motor.Velocity.y <= 0.5f;
            bool feetAbove = me.min.y > enemy.StompLineY;
            if (enemy.CanBeStomped && descending && feetAbove)
            {
                enemy.Stomp();
                var reward = _stompChain.Next();
                var popupAt = enemy.transform.position + Vector3.up;
                if (reward.ExtraLife)
                {
                    _ctx.Session.AddLife();
                    _ctx.Audio.Play(Sfx.OneUp);
                    _ctx.RaisePopup(popupAt, "1UP");
                }
                else
                {
                    _ctx.Session.AddScore(reward.Points);
                    _ctx.RaisePopup(popupAt, reward.Points.ToString());
                }
                _ctx.Audio.Play(Sfx.Stomp);
                Motor.Bounce(_frame.JumpHeld);
                _rb.linearVelocity = Motor.Velocity;
                _lastCommandedVy = Motor.Velocity.y;
                _visual.OnJump();
                return;
            }
            if (!IsInvincible) TakeHit();
        }

        public void TakeHit()
        {
            if (State != LifeState.Alive || IsInvincible) return;
            if (IsBig)
            {
                SetBig(false);
                InvincibleTime = HurtInvincibility;
                _ctx.Audio.Play(Sfx.Shrink);
                return;
            }
            Die();
        }

        public void CollectPowerUp()
        {
            _ctx.Session.AddScore(1000);
            _ctx.Audio.Play(Sfx.PowerUp);
            _ctx.RaisePopup(transform.position + Vector3.up * 1.6f, "1000");
            if (!IsBig) SetBig(true);
        }

        void SetBig(bool big)
        {
            if (IsBig == big) return;
            float from = transform.localScale.y;
            IsBig = big;
            float to = big ? BigScale : 1f;
            transform.localScale = new Vector3(to, to, 1f);
            _visual.PlayResizeFlicker(from / to);
        }

        public void Die()
        {
            if (State != LifeState.Alive) return;
            State = LifeState.Dying;
            _rb.simulated = false;
            Motor.Reset();
            InvincibleTime = 0f;
            _visual.ShowDeath();
            _ctx.RaisePlayerDied();
            StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            yield return new WaitForSeconds(0.5f);
            float vy = 13f;
            for (float t = 0f; t < 3f; t += Time.deltaTime)
            {
                vy -= 35f * Time.deltaTime;
                transform.position += Vector3.up * (vy * Time.deltaTime);
                yield return null;
            }
        }

        public void BeginGoal(GoalPole pole)
        {
            if (State != LifeState.Alive) return;
            State = LifeState.Goal;
            _rb.simulated = false;
            Motor.Reset();
            InvincibleTime = 0f;
            StartCoroutine(GoalRoutine(pole));
        }

        IEnumerator GoalRoutine(GoalPole pole)
        {
            _ctx.Audio.Play(Sfx.Flagpole);
            Facing = 1;
            _visual.SetPose(PlayerVisual.Pose.Climb);
            float halfWidth = ColliderSize.x * 0.5f * transform.localScale.x;
            var pos = transform.position;
            if (pos.y >= pole.BaseY - 0.01f) pos.x = pole.X - halfWidth + 0.02f; // hug the pole
            transform.position = pos;

            while (transform.position.y > pole.BaseY)
            {
                pos.y = Mathf.Max(pole.BaseY, pos.y - GoalPole.SlideSpeed * Time.deltaTime);
                transform.position = pos;
                yield return null;
            }
            yield return new WaitForSeconds(0.3f);

            // Hop off to the right onto the ground beside the base block.
            var start = transform.position;
            var hit = Physics2D.Raycast(new Vector2(pole.X + 1.6f, pole.BaseY + 0.5f), Vector2.down, 20f, Layers.GroundMask);
            var end = new Vector3(pole.X + 1.6f, hit.collider != null ? hit.point.y : pole.BaseY - 1f, 0f);
            _visual.SetPose(PlayerVisual.Pose.Jump);
            const float hopTime = 0.45f;
            for (float t = 0f; t < hopTime; t += Time.deltaTime)
            {
                float k = t / hopTime;
                transform.position = Vector3.Lerp(start, end, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 1.2f);
                yield return null;
            }
            transform.position = end;
            _visual.OnLand();
            _visual.SetPose(PlayerVisual.Pose.Win);
            FinishedGoal = true;
            _ctx.RaisePlayerFinishedGoal();
        }
    }
}
