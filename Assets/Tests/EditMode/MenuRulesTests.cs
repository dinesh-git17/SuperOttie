using NUnit.Framework;
using SuperOttie.Core;
using SuperOttie.UI;

namespace SuperOttie.Tests
{
    public class CourseProgressTests
    {
        [Test]
        public void FreshProgress_OnlyFirstCourseIsOpen()
        {
            var p = new CourseProgress(6);
            Assert.That(p.UnlockedCount, Is.EqualTo(1));
            Assert.That(p.IsUnlocked(0), Is.True);
            Assert.That(p.IsUnlocked(1), Is.False);
            Assert.That(p.ClearedCount, Is.Zero);
        }

        [Test]
        public void ClearingACourse_OpensTheNextOne()
        {
            var p = new CourseProgress(6);
            Assert.That(p.MarkCleared(0), Is.True);
            Assert.That(p.IsCleared(0), Is.True);
            Assert.That(p.IsUnlocked(1), Is.True);
            Assert.That(p.IsUnlocked(2), Is.False);
            Assert.That(p.UnlockedCount, Is.EqualTo(2));
        }

        [Test]
        public void ClearingTwice_ReportsNoChange()
        {
            var p = new CourseProgress(6);
            p.MarkCleared(0);
            Assert.That(p.MarkCleared(0), Is.False);
            Assert.That(p.ClearedCount, Is.EqualTo(1));
        }

        [Test]
        public void ClearingTheLastCourse_KeepsUnlockedCountInRange()
        {
            var p = new CourseProgress(3);
            for (int i = 0; i < 3; i++) p.MarkCleared(i);
            Assert.That(p.UnlockedCount, Is.EqualTo(3));
            Assert.That(p.AllCleared, Is.True);
            Assert.That(p.NextUp, Is.EqualTo(2), "with everything cleared, the last course is the suggestion");
        }

        [Test]
        public void FurthestClear_OpensEverythingBeforeIt()
        {
            // A gap (course 2 cleared, course 1 not) can appear if courses are reordered; never lock the player out.
            var p = new CourseProgress(6, clearedMask: 0b100);
            Assert.That(p.UnlockedCount, Is.EqualTo(4));
            Assert.That(p.IsUnlocked(1), Is.True);
            Assert.That(p.IsCleared(1), Is.False);
        }

        [Test]
        public void NextUp_IsTheFurthestOpenCourse()
        {
            var p = new CourseProgress(6, clearedMask: 0b11);
            Assert.That(p.NextUp, Is.EqualTo(2));
        }

        [Test]
        public void SavedBitsBeyondCourseCount_AreIgnored()
        {
            var p = new CourseProgress(3, clearedMask: 0b11111);
            Assert.That(p.ClearedMask, Is.EqualTo(0b111));
            Assert.That(p.ClearedCount, Is.EqualTo(3));
        }

        [Test]
        public void OutOfRangeIndices_AreNeitherOpenNorCleared()
        {
            var p = new CourseProgress(3, clearedMask: 0b111);
            Assert.That(p.IsUnlocked(-1) || p.IsUnlocked(3), Is.False);
            Assert.That(p.IsCleared(-1) || p.IsCleared(3), Is.False);
            Assert.That(p.MarkCleared(5), Is.False);
        }

        [Test]
        public void TooManyCourses_IsRejected()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new CourseProgress(CourseProgress.MaxCourses + 1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new CourseProgress(0));
        }
    }

    public class MenuFocusTests
    {
        [Test]
        public void StartsHidden_FirstMoveOnlyRevealsIt()
        {
            var f = new MenuFocus(3);
            Assert.That(f.Visible, Is.False);
            Assert.That(f.Move(0, 1), Is.True);
            Assert.That(f.Visible, Is.True);
            Assert.That(f.Index, Is.Zero, "the first press shows where you are instead of jumping");
        }

        [Test]
        public void List_MovesUpAndDownAndClampsAtEnds()
        {
            var f = new MenuFocus(3);
            f.Move(0, 1);
            f.Move(0, 1);
            f.Move(0, 1);
            Assert.That(f.Index, Is.EqualTo(2));
            Assert.That(f.Move(0, 1), Is.False);
            Assert.That(f.Index, Is.EqualTo(2));
            f.Move(0, -1);
            Assert.That(f.Index, Is.EqualTo(1));
            Assert.That(f.Move(1, 0), Is.False, "a single-column list ignores left/right");
        }

        [Test]
        public void Grid_MovesByRowAndColumn()
        {
            var f = new MenuFocus(6, columns: 3);
            f.Reset(0, visible: true);
            f.Move(1, 0);
            f.Move(1, 0);
            Assert.That(f.Index, Is.EqualTo(2));
            f.Move(0, 1);
            Assert.That(f.Index, Is.EqualTo(5));
            f.Move(-1, 0);
            Assert.That(f.Index, Is.EqualTo(4));
            Assert.That(f.Move(0, 1), Is.False);
        }

        [Test]
        public void Grid_PartialLastRow_LandsOnLastItem()
        {
            var f = new MenuFocus(5, columns: 3);
            f.Reset(2, visible: true);
            f.Move(0, 1);
            Assert.That(f.Index, Is.EqualTo(4));
        }

        [Test]
        public void Reset_ClampsIndexAndHides()
        {
            var f = new MenuFocus(3);
            f.Reset(9);
            Assert.That(f.Index, Is.EqualTo(2));
            Assert.That(f.Visible, Is.False);
            f.Reset(-4, visible: true);
            Assert.That(f.Index, Is.Zero);
            Assert.That(f.Visible, Is.True);
        }

        [Test]
        public void Hide_KeepsIndex()
        {
            var f = new MenuFocus(3);
            f.Reset(1, visible: true);
            f.Hide();
            Assert.That(f.Visible, Is.False);
            Assert.That(f.Index, Is.EqualTo(1));
        }
    }
}
