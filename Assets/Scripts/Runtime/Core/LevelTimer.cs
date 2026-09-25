using System;

namespace SuperOttie.Core
{
    /// <summary>
    /// Course countdown. Like the classics, one "time unit" is shorter than a real second.
    /// </summary>
    public sealed class LevelTimer
    {
        public const float UnitsPerSecond = 2.5f;
        public const int HurryThreshold = 100;

        float _remaining;
        bool _hurryRaised;
        bool _expiredRaised;

        public LevelTimer(int timeLimit) => Restart(timeLimit);

        public int TimeLimit { get; private set; }
        public float Remaining => _remaining;

        /// <summary>Whole units shown in the HUD (rounded up so "0" only appears at expiry).</summary>
        public int Display => (int)Math.Ceiling(Math.Max(0f, _remaining));

        public bool IsExpired => _remaining <= 0f;

        public event Action HurryUp;
        public event Action Expired;

        public void Restart(int timeLimit)
        {
            TimeLimit = Math.Max(1, timeLimit);
            _remaining = TimeLimit;
            _hurryRaised = TimeLimit <= HurryThreshold;
            _expiredRaised = false;
        }

        public void Tick(float deltaSeconds)
        {
            if (_expiredRaised || deltaSeconds <= 0f) return;
            _remaining -= deltaSeconds * UnitsPerSecond;
            if (!_hurryRaised && _remaining <= HurryThreshold)
            {
                _hurryRaised = true;
                HurryUp?.Invoke();
            }
            if (_remaining <= 0f)
            {
                _remaining = 0f;
                _expiredRaised = true;
                Expired?.Invoke();
            }
        }

        /// <summary>Consumes up to <paramref name="units"/> of remaining time (used for the end-of-level bonus count-down).</summary>
        public int Drain(int units)
        {
            int available = Display;
            int taken = Math.Min(units, available);
            _remaining = available - taken;
            return taken;
        }
    }
}
