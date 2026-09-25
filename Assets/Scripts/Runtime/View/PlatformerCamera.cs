using UnityEngine;

namespace SuperOttie.View
{
    /// <summary>
    /// Side-scroller camera: leads slightly in the running direction, follows vertically only when
    /// needed, and never shows anything outside the level bounds.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class PlatformerCamera : MonoBehaviour
    {
        public const float DefaultOrthographicSize = 6f;

        [SerializeField] float lookAhead = 2f;
        [SerializeField] float horizontalSmoothTime = 0.18f;
        [SerializeField] float verticalSmoothTime = 0.3f;

        Camera _cam;
        Transform _target;
        Rect _bounds;
        Vector2 _velocity;
        float _lead;

        public Camera Camera => _cam != null ? _cam : _cam = GetComponent<Camera>();

        void Awake()
        {
            _cam = GetComponent<Camera>();
            _cam.orthographic = true;
            _cam.orthographicSize = DefaultOrthographicSize;
        }

        /// <param name="bounds">World rect the camera must stay inside (x, y = bottom-left).</param>
        public void Follow(Transform target, Rect bounds, bool snap = true)
        {
            _target = target;
            _bounds = bounds;
            _lead = 0f;
            if (snap && target != null)
            {
                var p = Desired(target.position.x, target.position.y);
                transform.position = new Vector3(p.x, p.y, transform.position.z);
                _velocity = Vector2.zero;
            }
        }

        public void ClearTarget() => _target = null;

        void LateUpdate()
        {
            if (_target == null) return;
            var tp = _target.position;
            float moveDir = 0f;
            if (_target.TryGetComponent<Rigidbody2D>(out var rb) && rb.simulated) moveDir = Mathf.Clamp(rb.linearVelocity.x / 4f, -1f, 1f);
            _lead = Mathf.MoveTowards(_lead, moveDir * lookAhead, Time.deltaTime * 3f);

            var desired = Desired(tp.x + _lead, tp.y);
            var pos = transform.position;
            pos.x = Mathf.SmoothDamp(pos.x, desired.x, ref _velocity.x, horizontalSmoothTime);
            pos.y = Mathf.SmoothDamp(pos.y, desired.y, ref _velocity.y, verticalSmoothTime);
            var clamped = Clamp(new Vector2(pos.x, pos.y));
            transform.position = new Vector3(clamped.x, clamped.y, pos.z);
        }

        Vector2 Desired(float x, float y)
        {
            float halfH = Camera.orthographicSize;
            // Keep the floor near the bottom; only rise when Ottie climbs into the top third.
            float camY = Mathf.Max(_bounds.yMin + halfH, y + 1f - halfH * 0.3f);
            return Clamp(new Vector2(x, camY));
        }

        public Vector2 Clamp(Vector2 p) => ClampToBounds(p, _bounds, Camera.orthographicSize, Camera.aspect);

        /// <summary>Pure clamp used by tests: keeps the view rectangle inside <paramref name="bounds"/>.</summary>
        public static Vector2 ClampToBounds(Vector2 p, Rect bounds, float orthoSize, float aspect)
        {
            float halfH = orthoSize, halfW = orthoSize * aspect;
            float x = bounds.width <= halfW * 2f ? bounds.center.x : Mathf.Clamp(p.x, bounds.xMin + halfW, bounds.xMax - halfW);
            float y = bounds.height <= halfH * 2f ? bounds.yMin + halfH : Mathf.Clamp(p.y, bounds.yMin + halfH, bounds.yMax - halfH);
            return new Vector2(x, y);
        }
    }
}
