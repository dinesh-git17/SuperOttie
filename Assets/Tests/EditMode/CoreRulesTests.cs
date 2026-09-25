using NUnit.Framework;
using SuperOttie.Core;
using SuperOttie.Entities;

namespace SuperOttie.Tests
{
    public class GameSessionTests
    {
        [Test]
        public void NewSession_StartsWithDefaults()
        {
            var s = new GameSession();
            Assert.That(s.Lives, Is.EqualTo(GameSession.StartingLives));
            Assert.That(s.Coins, Is.Zero);
            Assert.That(s.Score, Is.Zero);
        }

        [Test]
        public void AddCoin_AddsCoinAndScore()
        {
            var s = new GameSession();
            s.AddCoin();
            Assert.That(s.Coins, Is.EqualTo(1));
            Assert.That(s.Score, Is.EqualTo(GameSession.CoinScore));
        }

        [Test]
        public void HundredCoins_GrantExtraLifeAndWrap()
        {
            var s = new GameSession();
            int extraLives = 0;
            s.ExtraLife += () => extraLives++;
            for (int i = 0; i < 100; i++) s.AddCoin();
            Assert.That(s.Coins, Is.Zero);
            Assert.That(s.Lives, Is.EqualTo(GameSession.StartingLives + 1));
            Assert.That(extraLives, Is.EqualTo(1));
        }

        [Test]
        public void LoseLife_ReportsGameOverOnLastLife()
        {
            var s = new GameSession();
            Assert.That(s.LoseLife(), Is.False);
            Assert.That(s.LoseLife(), Is.False);
            Assert.That(s.LoseLife(), Is.True);
            Assert.That(s.Lives, Is.Zero);
        }

        [Test]
        public void AddScore_IgnoresNonPositive()
        {
            var s = new GameSession();
            s.AddScore(-50);
            s.AddScore(0);
            Assert.That(s.Score, Is.Zero);
        }

        [Test]
        public void Reset_RestoresDefaultsAndRaisesChanged()
        {
            var s = new GameSession();
            s.AddCoin();
            s.LoseLife();
            s.LevelIndex = 2;
            bool changed = false;
            s.Changed += () => changed = true;
            s.Reset();
            Assert.That(changed, Is.True);
            Assert.That(s.Lives, Is.EqualTo(GameSession.StartingLives));
            Assert.That(s.Score + s.Coins + s.LevelIndex, Is.Zero);
        }
    }

    public class StompChainTests
    {
        [Test]
        public void ChainEscalatesThenAwardsLives()
        {
            var chain = new StompChain();
            int[] expected = { 100, 200, 400, 500, 800, 1000, 2000, 4000, 5000, 8000 };
            foreach (var points in expected)
            {
                var r = chain.Next();
                Assert.That(r.Points, Is.EqualTo(points));
                Assert.That(r.ExtraLife, Is.False);
            }
            Assert.That(chain.Next().ExtraLife, Is.True);
            Assert.That(chain.Next().ExtraLife, Is.True);
        }

        [Test]
        public void Reset_StartsOver()
        {
            var chain = new StompChain();
            chain.Next();
            chain.Next();
            chain.Reset();
            Assert.That(chain.Next().Points, Is.EqualTo(100));
        }
    }

    public class LevelTimerTests
    {
        [Test]
        public void CountsDownInGameUnits()
        {
            var t = new LevelTimer(300);
            t.Tick(1f);
            Assert.That(t.Remaining, Is.EqualTo(300 - LevelTimer.UnitsPerSecond).Within(1e-4));
            Assert.That(t.Display, Is.EqualTo(298));
        }

        [Test]
        public void RaisesHurryOnceAndExpiredOnce()
        {
            var t = new LevelTimer(110);
            int hurry = 0, expired = 0;
            t.HurryUp += () => hurry++;
            t.Expired += () => expired++;
            for (int i = 0; i < 1000; i++) t.Tick(0.1f);
            Assert.That(hurry, Is.EqualTo(1));
            Assert.That(expired, Is.EqualTo(1));
            Assert.That(t.IsExpired, Is.True);
            Assert.That(t.Display, Is.Zero);
        }

        [Test]
        public void ShortTimer_DoesNotRaiseHurry()
        {
            var t = new LevelTimer(50);
            bool hurry = false;
            t.HurryUp += () => hurry = true;
            t.Tick(5f);
            Assert.That(hurry, Is.False);
        }

        [Test]
        public void Drain_TakesAtMostWhatRemains()
        {
            var t = new LevelTimer(10);
            Assert.That(t.Drain(4), Is.EqualTo(4));
            Assert.That(t.Display, Is.EqualTo(6));
            Assert.That(t.Drain(100), Is.EqualTo(6));
            Assert.That(t.Display, Is.Zero);
        }
    }

    public class GoalPoleScoreTests
    {
        [TestCase(7.5f, 5000)]
        [TestCase(5.2f, 2000)]
        [TestCase(3.6f, 800)]
        [TestCase(2.0f, 400)]
        [TestCase(0.3f, 100)]
        public void HigherGrabsScoreMore(float height, int expected) =>
            Assert.That(GoalPole.ScoreForHeight(height), Is.EqualTo(expected));
    }
}
