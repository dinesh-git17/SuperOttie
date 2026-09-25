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
        static Sprite _puff;

        /// <summary>Soft white disc generated once at runtime (no texture asset needed).</summary>
        static Sprite Puff
        {
            get
            {
                if (_puff != null) return _puff;
                const int size = 64;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                    byte a = (byte)(Mathf.Clamp01((1f - d) * 3f) * 255);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
                tex.SetPixels32(pixels);
                tex.Apply(false, true);
                _puff = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 128f);
                return _puff;
            }
        }

        /// <summary>Little dust clouds kicked up at Ottie's feet (landing, jumping, stomping).</summary>
        public static void Dust(Vector3 feet, Transform parent, int count = 4, float spread = 1f)
        {
            if (parent == null) return;
            for (int i = 0; i < count; i++)
            {
                float dir = count == 1 ? 0f : Mathf.Lerp(-1f, 1f, i / (float)(count - 1));
                var sr = SpriteObjects.Create("Dust", Puff, parent, feet + new Vector3(dir * 0.2f, 0.08f), Sorting.Effects);
                sr.color = new Color(1f, 0.97f, 0.9f, 0.85f);
                var puff = sr.gameObject.AddComponent<PuffMotion>();
                puff.Velocity = new Vector2(dir * 1.8f * spread, 0.6f + Random.value * 0.5f);
            }
        }

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

    sealed class PuffMotion : MonoBehaviour
    {
        public Vector2 Velocity;
        float _age;
        SpriteRenderer _sr;

        void Awake() => _sr = GetComponent<SpriteRenderer>();

        void Update()
        {
            const float life = 0.4f;
            _age += Time.deltaTime;
            float k = _age / life;
            if (k >= 1f)
            {
                Destroy(gameObject);
                return;
            }
            transform.position += (Vector3)(Velocity * Time.deltaTime);
            Velocity *= 1f - 4f * Time.deltaTime;
            transform.localScale = Vector3.one * Mathf.Lerp(0.25f, 0.6f, k);
            var c = _sr.color;
            c.a = 0.85f * (1f - k);
            _sr.color = c;
        }
    }
}
