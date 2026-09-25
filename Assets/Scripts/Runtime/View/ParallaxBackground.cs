using UnityEngine;

namespace SuperOttie.View
{
    /// <summary>
    /// Endless horizontally-repeating backdrop that scrolls slower than the world.
    /// The sprite must tile horizontally (the art pipeline cross-fades its edges).
    /// </summary>
    public sealed class ParallaxBackground : MonoBehaviour
    {
        /// <summary>1 = glued to the camera (infinitely far), 0 = moves with the world.</summary>
        [Range(0f, 1f)] public float factor = 0.8f;

        Camera _cam;
        SpriteRenderer _sr;
        float _period;

        public static ParallaxBackground Create(Sprite sprite, Camera cam, Transform parent, int sortingOrder)
        {
            var go = new GameObject("Background");
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            var bg = go.AddComponent<ParallaxBackground>();
            bg._cam = cam;
            bg._sr = sr;
            bg.Fit();
            bg.LateUpdate();
            return bg;
        }

        /// <summary>Scales the backdrop to cover the camera's height and repeats it enough times to cover the width.</summary>
        void Fit()
        {
            var size = _sr.sprite.bounds.size;
            float viewH = _cam.orthographicSize * 2f;
            float scale = viewH * 1.08f / size.y;
            transform.localScale = new Vector3(scale, scale, 1f);
            _period = size.x * scale;
            float viewW = viewH * Mathf.Max(_cam.aspect, 2.4f);
            int copies = Mathf.CeilToInt(viewW / _period) + 2;
            _sr.size = new Vector2(size.x * copies, size.y);
        }

        public static float WrappedX(float camX, float factor, float period) =>
            camX * factor + period * Mathf.Round(camX * (1f - factor) / period);

        void LateUpdate()
        {
            if (_cam == null) return;
            var c = _cam.transform.position;
            transform.position = new Vector3(WrappedX(c.x, factor, _period), c.y, 10f);
        }
    }
}
