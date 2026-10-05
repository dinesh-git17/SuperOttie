using System;
using UnityEngine;

namespace SuperOttie.Core
{
    /// <summary>
    /// Which courses the player has cleared, kept across runs. Course 1 is always open and clearing a
    /// course opens the next one. Pure C# (a bit mask) so the unlock rules are unit-testable.
    /// </summary>
    public sealed class CourseProgress
    {
        /// <summary>The mask is an int, so one bit per course caps the count.</summary>
        public const int MaxCourses = 31;

        public int CourseCount { get; }
        public int ClearedMask { get; private set; }

        public CourseProgress(int courseCount, int clearedMask = 0)
        {
            if (courseCount < 1 || courseCount > MaxCourses) throw new ArgumentOutOfRangeException(nameof(courseCount));
            CourseCount = courseCount;
            ClearedMask = clearedMask & ((1 << courseCount) - 1);
        }

        public bool IsCleared(int index) => InRange(index) && (ClearedMask & (1 << index)) != 0;

        /// <summary>Everything up to one past the furthest cleared course is open, so a gap never locks anyone out.</summary>
        public bool IsUnlocked(int index) => InRange(index) && index < UnlockedCount;

        public int UnlockedCount
        {
            get
            {
                int furthest = -1;
                for (int i = 0; i < CourseCount; i++)
                    if ((ClearedMask & (1 << i)) != 0) furthest = i;
                return Math.Min(CourseCount, furthest + 2);
            }
        }

        public int ClearedCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < CourseCount; i++)
                    if ((ClearedMask & (1 << i)) != 0) n++;
                return n;
            }
        }

        public bool AllCleared => ClearedCount == CourseCount;

        /// <summary>The course to suggest: the furthest one that's open.</summary>
        public int NextUp => UnlockedCount - 1;

        /// <summary>Records a clear. Returns true when it is new (so the caller knows to save).</summary>
        public bool MarkCleared(int index)
        {
            if (!InRange(index) || IsCleared(index)) return false;
            ClearedMask |= 1 << index;
            return true;
        }

        bool InRange(int index) => index >= 0 && index < CourseCount;
    }

    public interface ICourseProgressStore
    {
        int Load();
        void Save(int clearedMask);
    }

    public sealed class PlayerPrefsCourseProgressStore : ICourseProgressStore
    {
        public const string Key = "superottie.cleared";

        public int Load() => PlayerPrefs.GetInt(Key, 0);

        public void Save(int clearedMask)
        {
            PlayerPrefs.SetInt(Key, clearedMask);
            PlayerPrefs.Save();
        }
    }
}
