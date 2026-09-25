using System;

namespace SuperOttie.Core
{
    /// <summary>
    /// Run-wide progress (lives, coins, score, current level). Pure C# so the rules are unit-testable.
    /// </summary>
    public sealed class GameSession
    {
        public const int StartingLives = 3;
        public const int MaxLives = 99;
        public const int CoinsPerExtraLife = 100;
        public const int CoinScore = 200;

        public int Lives { get; private set; }
        public int Coins { get; private set; }
        public int Score { get; private set; }
        public int LevelIndex { get; set; }

        /// <summary>Raised whenever lives, coins or score change.</summary>
        public event Action Changed;

        /// <summary>Raised when an extra life is awarded (100 coins, stomp chains, ...).</summary>
        public event Action ExtraLife;

        public GameSession() => Reset();

        public void Reset()
        {
            Lives = StartingLives;
            Coins = 0;
            Score = 0;
            LevelIndex = 0;
            Changed?.Invoke();
        }

        public void AddScore(int amount)
        {
            if (amount <= 0) return;
            Score += amount;
            Changed?.Invoke();
        }

        public void AddCoin()
        {
            Coins++;
            Score += CoinScore;
            if (Coins >= CoinsPerExtraLife)
            {
                Coins -= CoinsPerExtraLife;
                AddLife();
            }
            Changed?.Invoke();
        }

        public void AddLife()
        {
            Lives = Math.Min(Lives + 1, MaxLives);
            ExtraLife?.Invoke();
            Changed?.Invoke();
        }

        /// <summary>Removes a life. Returns true when that was the last one (game over).</summary>
        public bool LoseLife()
        {
            Lives = Math.Max(Lives - 1, 0);
            Changed?.Invoke();
            return Lives == 0;
        }
    }
}
