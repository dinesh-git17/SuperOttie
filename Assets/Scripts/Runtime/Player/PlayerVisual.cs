using SuperOttie.Entities;
using SuperOttie.Game;
using UnityEngine;

namespace SuperOttie.Player
{
    /// <summary>
    /// Picks Ottie's sprite each frame and layers on juice: squash and stretch, idle breathing,
    /// hurt blinking and the grow/shrink flicker. Purely cosmetic; never touches physics.
    /// </summary>
    public sealed class PlayerVisual : MonoBehaviour
    {
        public enum Pose
        {
            Auto,
            Climb,
            Jump,
            Win,
            Hurt,
        }

        const float FlickerDuration = 0.7f;

        SpriteRenderer _sr;
        GameAssets _assets;
        Pose _pose = Pose.Auto;
        Vector2 _squash = Vector2.one;
        float _runPhase;
        float _breathe;
        float _flickerTime;
        float _flickerRatio = 1f;

        public SpriteRenderer Renderer => _sr;

        public void Init(SpriteRenderer sr, GameAssets assets)
        {
            _sr = sr;
            _assets = assets;
        }

        public void SetPose(Pose pose) => _pose = pose;

        public void OnJump() => _squash = new Vector2(0.8f, 1.2f);

        public void OnLand() => _squash = new Vector2(1.2f, 0.82f);

        /// <summary>Alternate between the old and new size for a moment (ratio = old/new scale).</summary>
        public void PlayResizeFlicker(float ratio)
        {
            _flickerRatio = ratio;
            _flickerTime = FlickerDuration;
        }

        public void ShowDeath()
        {
            _pose = Pose.Hurt;
            _sr.enabled = true;
            _sr.sortingOrder = Sorting.DyingPlayer;
            _squash = Vector2.one;
            _sr.transform.localScale = Vector3.one;
            ApplySprite(_assets.playerHurt);
        }

        public void Tick(PlayerController p, float dt)
        {
            if (_pose == Pose.Hurt) return;
            _sr.flipX = p.Facing < 0;

            Sprite sprite;
            float breatheScale = 1f;
            switch (_pose)
            {
                case Pose.Climb:
                case Pose.Jump:
                    sprite = _assets.playerJump;
                    break;
                case Pose.Win:
                    sprite = _assets.playerWin;
                    break;
                default:
                    var v = p.Velocity;
                    if (!p.IsGrounded) sprite = v.y > 0f ? _assets.playerJump : _assets.playerFall;
                    else if (Mathf.Abs(v.x) > 0.3f && _assets.playerRun.Length > 0)
                    {
                        _runPhase += dt * (5f + Mathf.Abs(v.x) * 1.4f);
                        sprite = _assets.playerRun[(int)_runPhase % _assets.playerRun.Length];
                    }
                    else
                    {
                        _runPhase = 0f;
                        _breathe += dt;
                        breatheScale = 1f + Mathf.Sin(_breathe * 3f) * 0.025f;
                        sprite = _assets.playerIdle;
                    }
                    break;
            }
            ApplySprite(sprite);

            _squash = Vector2.Lerp(_squash, Vector2.one, 1f - Mathf.Exp(-14f * dt));

            float flicker = 1f;
            if (_flickerTime > 0f)
            {
                _flickerTime -= dt;
                flicker = Mathf.Repeat(_flickerTime, 0.14f) < 0.07f ? _flickerRatio : 1f;
            }

            var child = _sr.transform;
            child.localScale = new Vector3(_squash.x * flicker * (2f - breatheScale), _squash.y * flicker * breatheScale, 1f);

            // Blink while invincible after getting hurt.
            _sr.enabled = !p.IsInvincible || Mathf.Repeat(p.InvincibleTime, 0.14f) > 0.06f;
        }

        void ApplySprite(Sprite s)
        {
            if (s != null) _sr.sprite = s;
        }
    }
}
