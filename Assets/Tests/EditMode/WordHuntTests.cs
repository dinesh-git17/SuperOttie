using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SuperOttie.UI;
using SuperOttie.WordHunt;
using UnityEngine;

namespace SuperOttie.Tests
{
    public class WordListTests
    {
        [Test]
        public void Parse_SortsLowercasesAndSkipsBlanks()
        {
            var list = WordList.Parse("tree\r\nCat\n\n  dog \n");
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list.Contains("cat"), Is.True);
            Assert.That(list.Contains("dog"), Is.True);
            Assert.That(list.Contains("cow"), Is.False);
        }

        [Test]
        public void HasPrefix_FindsStartsOfWords()
        {
            var list = WordList.Parse("otter\notters\nstone");
            Assert.That(list.HasPrefix("ott"), Is.True);
            Assert.That(list.HasPrefix("otter"), Is.True);
            Assert.That(list.HasPrefix("st"), Is.True);
            Assert.That(list.HasPrefix("ox"), Is.False);
            Assert.That(list.HasPrefix("stones"), Is.False);
        }

        [Test]
        public void ShippedDictionary_HasEverydayWordsAndNoBlockedOnes()
        {
            var list = WordList.Parse(File.ReadAllText("Assets/Data/words.txt"));
            Assert.That(list.Count, Is.GreaterThan(100000));
            foreach (var w in new[] { "cat", "cats", "trees", "jumped", "otter", "stones", "run" })
                Assert.That(list.Contains(w), Is.True, w);
            foreach (var w in new[] { "sex", "shit", "damn" })
                Assert.That(list.Contains(w), Is.False, w);
        }
    }

    public class LetterGridTests
    {
        //  c a t s
        //  x o x e
        //  d g x a
        //  x x x x
        static LetterGrid Grid() => new LetterGrid("catsxoxedgxaxxxx");

        [Test]
        public void Adjacent_IncludesDiagonalsButNotSelfOrDistant()
        {
            Assert.That(LetterGrid.Adjacent(0, 1), Is.True);
            Assert.That(LetterGrid.Adjacent(0, 5), Is.True, "diagonal");
            Assert.That(LetterGrid.Adjacent(0, 4), Is.True, "below");
            Assert.That(LetterGrid.Adjacent(0, 0), Is.False);
            Assert.That(LetterGrid.Adjacent(0, 2), Is.False);
            Assert.That(LetterGrid.Adjacent(3, 4), Is.False, "no wrapping from the end of a row to the next");
        }

        [Test]
        public void Constructor_RejectsWrongSizeOrNonLetters()
        {
            Assert.Throws<System.ArgumentException>(() => new LetterGrid("abc"));
            Assert.Throws<System.ArgumentException>(() => new LetterGrid("abcdefghijklmno1"));
        }

        [Test]
        public void FindPath_TracesAWordThroughNeighbours()
        {
            var path = Grid().FindPath("dog");
            Assert.That(path, Is.EqualTo(new List<int> { 8, 5, 9 }));
            Assert.That(Grid().FindPath("cats"), Is.EqualTo(new List<int> { 0, 1, 2, 3 }));
        }

        [Test]
        public void FindPath_ReturnsNullWhenLettersAreNotConnected()
        {
            Assert.That(Grid().FindPath("cad"), Is.Null, "the a next to c isn't next to d");
        }

        [Test]
        public void FindPath_NeverReusesATile()
        {
            Assert.That(new LetterGrid("abaxxxxxxxxxxxxx").FindPath("aba"), Is.Not.Null);
            Assert.That(new LetterGrid("abxxxxxxxxxxxxxx").FindPath("aba"), Is.Null);
        }

        [Test]
        public void Solver_FindsEveryDictionaryWordOnTheBoard()
        {
            var words = WordList.Parse("cat\ncats\ndog\nsat\nsea\nseat\ntoe\nzebra");
            var found = GridSolver.FindAll(Grid(), words);
            Assert.That(found, Is.EquivalentTo(new[] { "cat", "cats", "dog", "sea" }), "seat: the t isn't next to that a");
        }

        [Test]
        public void Generator_BoardsAlwaysHavePlentyOfWords()
        {
            var words = WordList.Parse(File.ReadAllText("Assets/Data/words.txt"));
            for (int seed = 0; seed < 25; seed++)
            {
                var grid = GridGenerator.Generate(words, new System.Random(seed));
                int count = GridSolver.FindAll(grid, words).Count;
                Assert.That(count, Is.GreaterThanOrEqualTo(GridGenerator.MinWords), $"seed {seed}: {grid}");
                Assert.That(grid.ToString(), Does.Not.Contain("q"), "Word Hunt has no Qu tile");
            }
        }

        [Test]
        public void Generator_IsRepeatableForASeed()
        {
            var words = WordList.Parse(File.ReadAllText("Assets/Data/words.txt"));
            var a = GridGenerator.Generate(words, new System.Random(7));
            var b = GridGenerator.Generate(words, new System.Random(7));
            Assert.That(a.ToString(), Is.EqualTo(b.ToString()));
        }
    }

    public class WordHuntRoundTests
    {
        static readonly WordList Words = WordList.Parse("cat\ncats\ndog\nsea\nseat\neat\ngod");

        //  c a t s
        //  x o x e
        //  d g x a
        //  x x x x
        static WordHuntRound Round() => new WordHuntRound(new LetterGrid("catsxoxedgxaxxxx"), Words);

        static void Trace(WordHuntRound r, params int[] cells)
        {
            foreach (int c in cells) r.Touch(c);
        }

        [Test]
        public void Touch_BuildsAPathThroughNeighbours()
        {
            var r = Round();
            Assert.That(r.Touch(0), Is.True);
            Assert.That(r.Touch(1), Is.True);
            Assert.That(r.Touch(3), Is.False, "not next to the last tile");
            Assert.That(r.Touch(2), Is.True);
            Assert.That(r.CurrentWord, Is.EqualTo("cat"));
        }

        [Test]
        public void Touch_SameTileTwiceIsIgnored_AndPreviousTileBacktracks()
        {
            var r = Round();
            Trace(r, 0, 1, 2);
            Assert.That(r.Touch(2), Is.False);
            Assert.That(r.Touch(1), Is.True, "sliding back to the previous tile undoes the last letter");
            Assert.That(r.CurrentWord, Is.EqualTo("ca"));
            Trace(r, 5);
            Assert.That(r.Touch(0), Is.False, "a tile already in the path (not the previous one) is ignored");
            Assert.That(r.CurrentWord, Is.EqualTo("cao"));
        }

        [Test]
        public void PathState_ShowsNewAndAlreadyFoundWords()
        {
            var r = Round();
            Trace(r, 0, 1);
            Assert.That(r.State, Is.EqualTo(PathState.Neutral));
            Trace(r, 2);
            Assert.That(r.State, Is.EqualTo(PathState.NewWord));
            r.Release();
            Trace(r, 0, 1, 2);
            Assert.That(r.State, Is.EqualTo(PathState.AlreadyFound));
        }

        [Test]
        public void Release_ScoresNewWordsOnce()
        {
            var r = Round();
            Trace(r, 0, 1, 2, 3);
            Assert.That(r.Release(), Is.EqualTo(SubmitResult.Found));
            Assert.That(r.Found, Is.EqualTo(new[] { "cats" }));
            Assert.That(r.Points, Is.EqualTo(WordHuntRound.PointsFor(4)));
            Assert.That(r.Path, Is.Empty);

            Trace(r, 0, 1, 2, 3);
            Assert.That(r.Release(), Is.EqualTo(SubmitResult.AlreadyFound));
            Assert.That(r.Found.Count, Is.EqualTo(1));
        }

        [Test]
        public void Release_RejectsShortAndUnknownWords()
        {
            var r = Round();
            Trace(r, 0, 1);
            Assert.That(r.Release(), Is.EqualTo(SubmitResult.TooShort));
            Trace(r, 0, 5, 9);
            Assert.That(r.Release(), Is.EqualTo(SubmitResult.NotAWord));
            Assert.That(r.Release(), Is.EqualTo(SubmitResult.None), "nothing traced");
            Assert.That(r.Found, Is.Empty);
        }

        [Test]
        public void ThreeWords_WinTheRound()
        {
            var r = Round();
            Trace(r, 0, 1, 2);
            r.Release();
            Trace(r, 8, 5, 9);
            r.Release();
            Assert.That(r.IsWon, Is.False);
            Trace(r, 3, 7, 11);
            Assert.That(r.Release(), Is.EqualTo(SubmitResult.Found));
            Assert.That(r.IsWon, Is.True);
            Assert.That(r.IsOver, Is.True);
        }

        [Test]
        public void RunningOutOfTime_LosesTheRound_AndStopsInput()
        {
            var r = Round();
            r.Tick(WordHuntRound.DefaultTimeLimit - 1f);
            Assert.That(r.TimeLeft, Is.EqualTo(1f).Within(1e-4));
            r.Tick(5f);
            Assert.That(r.TimeLeft, Is.Zero);
            Assert.That(r.IsLost, Is.True);
            Assert.That(r.Touch(0), Is.False);
            Assert.That(r.SubmitTyped("cat"), Is.EqualTo(SubmitResult.None));
        }

        [Test]
        public void WinningStopsTheClock()
        {
            var r = Round();
            r.SubmitTyped("cat");
            r.SubmitTyped("dog");
            r.SubmitTyped("sea");
            float left = r.TimeLeft;
            r.Tick(10f);
            Assert.That(r.TimeLeft, Is.EqualTo(left));
        }

        [Test]
        public void SubmitTyped_ChecksTheWordIsOnTheBoard()
        {
            var r = Round();
            Assert.That(r.SubmitTyped("Dog"), Is.EqualTo(SubmitResult.Found));
            Assert.That(r.SubmitTyped("eat"), Is.EqualTo(SubmitResult.NotOnBoard), "letters there but not connected in that order");
            Assert.That(r.SubmitTyped("zzz"), Is.EqualTo(SubmitResult.NotOnBoard));
            Assert.That(r.SubmitTyped("ca"), Is.EqualTo(SubmitResult.TooShort));
            Assert.That(r.SubmitTyped("dog"), Is.EqualTo(SubmitResult.AlreadyFound));
        }

        [Test]
        public void Points_FollowWordLength()
        {
            Assert.That(WordHuntRound.PointsFor(2), Is.Zero);
            Assert.That(WordHuntRound.PointsFor(3), Is.EqualTo(100));
            Assert.That(WordHuntRound.PointsFor(4), Is.EqualTo(400));
            Assert.That(WordHuntRound.PointsFor(5), Is.EqualTo(800));
            Assert.That(WordHuntRound.PointsFor(6), Is.EqualTo(1400));
            Assert.That(WordHuntRound.PointsFor(8), Is.EqualTo(2200));
        }
    }

    public class WordHuntViewTests
    {
        static Vector2 Center(int cell) => new Vector2(
            WordHuntView.BoardPadding + LetterGrid.Column(cell) * (WordHuntView.TileSize + WordHuntView.TileGap) + WordHuntView.TileSize / 2f,
            WordHuntView.BoardPadding + LetterGrid.Row(cell) * (WordHuntView.TileSize + WordHuntView.TileGap) + WordHuntView.TileSize / 2f);

        [Test]
        public void TileCentres_MapToTheirCells()
        {
            for (int cell = 0; cell < LetterGrid.CellCount; cell++)
                Assert.That(WordHuntView.CellAtLocal(Center(cell)), Is.EqualTo(cell));
        }

        [Test]
        public void DiagonalDrag_PassesBetweenTheSideNeighbours()
        {
            // Halfway from tile 0 to tile 5 is the corner shared with tiles 1 and 4; it must not select any of them.
            var mid = (Center(0) + Center(5)) / 2f;
            Assert.That(WordHuntView.CellAtLocal(mid), Is.EqualTo(-1));
            var nearFive = Vector2.Lerp(Center(0), Center(5), 0.8f);
            Assert.That(WordHuntView.CellAtLocal(nearFive), Is.EqualTo(5));
        }

        [Test]
        public void GapsAndOutsideTheBoard_HitNothing()
        {
            Assert.That(WordHuntView.CellAtLocal((Center(0) + Center(1)) / 2f), Is.EqualTo(-1));
            Assert.That(WordHuntView.CellAtLocal(new Vector2(-50f, -50f)), Is.EqualTo(-1));
        }
    }
}
