using System.IO;
using System.Linq;
using NUnit.Framework;
using SuperOttie.Level;
using UnityEngine;

namespace SuperOttie.Tests
{
    public class LevelParserTests
    {
        const string Small =
            "name: Test Course\n" +
            "theme: sunset\n" +
            "time: 150\n" +
            "---\n" +
            "...c...?M.B.....\n" +
            "................\n" +
            "..P...e....[]..F\n" +
            "######.....||S##\n" +
            "################\n";

        [Test]
        public void Header_IsRead()
        {
            var level = LevelParser.Parse(Small);
            Assert.That(level.Name, Is.EqualTo("Test Course"));
            Assert.That(level.Theme, Is.EqualTo("sunset"));
            Assert.That(level.TimeLimit, Is.EqualTo(150));
        }

        [Test]
        public void Grid_IsBottomUp()
        {
            var level = LevelParser.Parse(Small);
            Assert.That(level.Width, Is.EqualTo(16));
            Assert.That(level.Height, Is.EqualTo(5));
            Assert.That(level.GetTile(0, 0), Is.EqualTo(TileKind.Ground));
            Assert.That(level.GetTile(6, 1), Is.EqualTo(TileKind.Empty));
            Assert.That(level.GetTile(13, 1), Is.EqualTo(TileKind.Stone));
            Assert.That(level.GetTile(-1, 0), Is.EqualTo(TileKind.Empty), "out of range reads as empty");
        }

        [Test]
        public void Markers_BecomeSpawns()
        {
            var level = LevelParser.Parse(Small);
            Assert.That(level.PlayerStart, Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(level.FlagCell, Is.EqualTo(new Vector2Int(15, 2)));
            Assert.That(level.GetTile(15, 2), Is.EqualTo(TileKind.Stone), "flag stands on a stone base");

            var kinds = level.Spawns.Select(s => s.Kind).ToList();
            CollectionAssert.Contains(kinds, SpawnKind.Coin);
            CollectionAssert.Contains(kinds, SpawnKind.QuestionCoin);
            CollectionAssert.Contains(kinds, SpawnKind.QuestionPowerUp);
            CollectionAssert.Contains(kinds, SpawnKind.Brick);
            CollectionAssert.Contains(kinds, SpawnKind.Crab);

            var pipe = level.Spawns.Single(s => s.Kind == SpawnKind.Pipe);
            Assert.That(pipe.Cell, Is.EqualTo(new Vector2Int(11, 2)));
            Assert.That(pipe.Size, Is.EqualTo(2));
        }

        [Test]
        public void ShortRows_ArePaddedWithEmptyCells()
        {
            var level = LevelParser.Parse("---\n.......c\nP..F\n####\n");
            Assert.That(level.Width, Is.EqualTo(8));
            Assert.That(level.GetTile(6, 0), Is.EqualTo(TileKind.Empty));
        }

        [Test]
        public void DefaultsApplyWithoutHeader()
        {
            var level = LevelParser.Parse("---\nP.F\n###\n");
            Assert.That(level.TimeLimit, Is.EqualTo(LevelParser.DefaultTime));
            Assert.That(level.Theme, Is.EqualTo("day"));
        }

        [TestCase("---\n....\n####\n", "no player")]
        [TestCase("---\nP...\n####\n", "no flag")]
        [TestCase("---\nPP.F\n####\n", "two players")]
        [TestCase("---\nP.xF\n####\n", "unknown char")]
        [TestCase("P.F\n###\n", "missing separator")]
        [TestCase("time: soon\n---\nP.F\n###\n", "bad time")]
        [TestCase("---\nP[.F\n####\n", "unclosed pipe")]
        [TestCase("---\nP[]F\n#..#\n", "floating pipe")]
        [TestCase("colour: red\n---\nP.F\n###\n", "unknown key")]
        [TestCase("", "empty")]
        public void InvalidLevels_Throw(string text, string why) =>
            Assert.Throws<LevelFormatException>(() => LevelParser.Parse(text), why);

        [Test]
        public void WindowsLineEndings_Parse()
        {
            var level = LevelParser.Parse("name: A\r\n---\r\nP.F\r\n###\r\n");
            Assert.That(level.Width, Is.EqualTo(3));
        }
    }

    /// <summary>Every shipped level must parse and pass the beatability lint.</summary>
    public class BundledLevelTests
    {
        static string[] LevelFiles() => Directory.GetFiles("Assets/Levels", "level*.txt").OrderBy(p => p).ToArray();

        [Test]
        public void ThereAreAtLeastThreeLevels() => Assert.That(LevelFiles().Length, Is.GreaterThanOrEqualTo(3));

        [TestCaseSource(nameof(LevelFiles))]
        public void Level_ParsesAndPassesLint(string path)
        {
            var level = LevelParser.Parse(File.ReadAllText(path));
            var problems = LevelLint.Validate(level);
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
            Assert.That(level.Spawns.Count(s => s.Kind == SpawnKind.QuestionPowerUp), Is.GreaterThan(0), "each level offers a power-up");
            Assert.That(new[] { "day", "sunset", "twilight" }, Does.Contain(level.Theme));
        }
    }

    public class LevelLintTests
    {
        [Test]
        public void WidePit_IsReported()
        {
            var level = LevelParser.Parse("---\nP.........F\n##......###\n");
            Assert.That(LevelLint.Validate(level), Has.Some.Contains("Pit"));
        }

        [Test]
        public void TallWall_IsReported()
        {
            var level = LevelParser.Parse("---\n..S....\n..S....\n..S....\n..S....\n..S....\nP.S...F\n#######\n");
            Assert.That(LevelLint.Validate(level), Has.Some.Contains("Step up"));
        }

        [Test]
        public void ReasonableLevel_IsClean()
        {
            var level = LevelParser.Parse("---\n..?.......\nP..e.S...F\n###..#####\n");
            Assert.That(LevelLint.Validate(level), Is.Empty);
        }
    }
}
