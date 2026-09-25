using System.Collections;
using UnityEngine;

namespace SuperOttie.Entities
{
    /// <summary>Simple projectile motion for cosmetic pieces (debris, knocked-out enemies).</summary>
    public sealed class Ballistic : MonoBehaviour
    {
        public Vector2 Velocity;
        public float Gravity = 32f;
        public float Spin;
        public float KillY = -3f;
        public float Lifetime = 4f;

        void Update()
        {
            float dt = Time.deltaTime;
            Velocity.y -= Gravity * dt;
            transform.position += (Vector3)(Velocity * dt);
            if (Spin != 0f) transform.Rotate(0f, 0f, Spin * dt);
            Lifetime -= dt;
            if (transform.position.y < KillY || Lifetime <= 0f) Destroy(gameObject);
        }
    }

    public static class Effects
    {
        /// <summary>The coin that pops out of a "?" block, spins up and vanishes.</summary>
        public static void CoinPop(Sprite coin, Vector3 blockCenter, Transform parent)
        {
            var sr = SpriteObjects.Create("CoinPop", coin, parent, blockCenter + Vector3.up * 0.6f, Sorting.Effects);
            sr.gameObject.AddComponent<CoinPopMotion>();
        }

        public static void BrickDebris(Sprite brick, Vector3 center, Transform parent, float killY)
        {
            var dirs = new[] { new Vector2(-2.6f, 11f), new Vector2(2.6f, 11f), new Vector2(-2.2f, 7f), new Vector2(2.2f, 7f) };
            for (int i = 0; i < dirs.Length; i++)
            {
                var offset = new Vector3(i % 2 == 0 ? -0.25f : 0.25f, i < 2 ? 0.25f : -0.25f);
                var sr = SpriteObjects.Create("Debris", brick, parent, center + offset, Sorting.Effects);
                sr.transform.localScale = Vector3.one * 0.45f;
                var b = sr.gameObject.AddComponent<Ballistic>();
                b.Velocity = dirs[i];
                b.Spin = i % 2 == 0 ? 540f : -540f;
                b.KillY = killY;
            }
        }
    }

    sealed class CoinPopMotion : MonoBehaviour
    {
        IEnumerator Start()
        {
            var start = transform.position;
            const float duration = 0.45f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                // Up fast, ease back down a little, spinning.
                float y = 2.4f * k - 1.6f * k * k;
                transform.position = start + Vector3.up * y;
                transform.localScale = new Vector3(Mathf.Abs(Mathf.Cos(t * 18f)) * 0.9f + 0.1f, 1f, 1f);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
