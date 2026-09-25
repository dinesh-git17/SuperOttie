namespace SuperOttie.Core
{
    public readonly struct StompReward
    {
        public readonly int Points;
        public readonly bool ExtraLife;

        public StompReward(int points, bool extraLife)
        {
            Points = points;
            ExtraLife = extraLife;
        }
    }

    /// <summary>
    /// Classic escalating reward for stomping several enemies without touching the ground.
    /// </summary>
    public sealed class StompChain
    {
        static readonly int[] Table = { 100, 200, 400, 500, 800, 1000, 2000, 4000, 5000, 8000 };

        int _count;

        public int Count => _count;

        public StompReward Next()
        {
            var reward = _count < Table.Length
                ? new StompReward(Table[_count], false)
                : new StompReward(0, true);
            _count++;
            return reward;
        }

        public void Reset() => _count = 0;
    }
}
