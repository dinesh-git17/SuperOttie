using System.Collections;
using System.Collections.Generic;
using SuperOttie.Audio;
using SuperOttie.Core;
using SuperOttie.Level;
using SuperOttie.Player;
using UnityEngine;

namespace SuperOttie.Entities
{
    /// <summary>Solid 1x1 block the player can hit from below.</summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public abstract class BlockBase : MonoBehaviour, IBumpable
    {
        const float BumpHeight = 0.3f;
        const float BumpTime = 0.16f;

        static readonly List<Collider2D> Above = new List<Collider2D>(8);

        protected LevelContext Ctx;
        protected SpriteRenderer Visual;
        bool _animating;

        public void Init(LevelContext ctx, SpriteRenderer visual)
        {
            Ctx = ctx;
            Visual = visual;
        }

        public abstract void Bump(PlayerController player);

        protected void PlayBumpAnimation()
        {
            if (!_animating) StartCoroutine(BumpRoutine());
        }

        IEnumerator BumpRoutine()
        {
            _animating = true;
            for (float t = 0f; t < BumpTime; t += Time.deltaTime)
            {
                Visual.transform.localPosition = Vector3.up * (Mathf.Sin(Mathf.PI * t / BumpTime) * BumpHeight);
                yield return null;
            }
            Visual.transform.localPosition = Vector3.zero;
            _animating = false;
        }

        /// <summary>Enemies standing on a bumped block get knocked out; coins on it are collected; fish hop.</summary>
        protected void AffectThingsAbove(PlayerController player)
        {
            var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = Layers.EnemyMask | Layers.ItemMask };
            Above.Clear();
            Physics2D.OverlapBox((Vector2)transform.position + new Vector2(0f, 0.6f), new Vector2(0.9f, 0.3f), 0f, filter, Above);
            int dir = player != null && player.transform.position.x < transform.position.x ? 1 : -1;
            foreach (var c in Above)
            {
                if (c == null) continue;
                var enemy = c.GetComponentInParent<Enemy>();
                if (enemy != null) enemy.KnockOut(dir);
                else if (c.TryGetComponent<PowerUpFish>(out var fish)) fish.Hop();
                else if (c.TryGetComponent<Coin>(out var coin)) coin.Collect(player);
            }
        }
    }

    public sealed class QuestionBlock : BlockBase
    {
        static readonly string CoinScoreText = Core.GameSession.CoinScore.ToString();

        public enum Content
        {
            Coin,
            PowerUp,
        }

        public Content Contents { get; set; }
        public bool IsUsed { get; private set; }

        public override void Bump(PlayerController player)
        {
            if (IsUsed)
            {
                Ctx.Audio.Play(Sfx.Bump);
                return;
            }
            IsUsed = true;
            if (Ctx.Assets.blockUsed != null) Visual.sprite = Ctx.Assets.blockUsed;
            PlayBumpAnimation();
            AffectThingsAbove(player);

            if (Contents == Content.Coin)
            {
                Ctx.Session.AddCoin();
                Ctx.Audio.Play(Sfx.Coin);
                Effects.CoinPop(Ctx.Assets.coin, transform.position, Ctx.Root);
                Ctx.RaisePopup(transform.position + Vector3.up * 1.8f, CoinScoreText);
            }
            else
            {
                Ctx.Audio.Play(Sfx.PowerUpAppear);
                PowerUpFish.Spawn(Ctx, transform.position, Ctx.Root);
            }
        }
    }

    public sealed class BrickBlock : BlockBase
    {
        public const int BreakScore = 50;

        public override void Bump(PlayerController player)
        {
            AffectThingsAbove(player);
            if (player != null && player.IsBig)
            {
                Ctx.Session.AddScore(BreakScore);
                Ctx.Audio.Play(Sfx.BrickBreak);
                Effects.BrickDebris(Ctx.Assets.blockBrick, transform.position, Ctx.Root, Ctx.KillY);
                GetComponent<Collider2D>().enabled = false;
                Destroy(gameObject);
                return;
            }
            Ctx.Audio.Play(Sfx.Bump);
            PlayBumpAnimation();
        }
    }
}
