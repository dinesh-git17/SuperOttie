using System;
using System.Collections.Generic;

namespace SuperOttie.WordHunt
{
    /// <summary>
    /// The Word Hunt dictionary: a sorted array searched with binary search, which answers both "is this a word"
    /// and "does any word start with this" (the solver's pruning test) without building a trie.
    /// </summary>
    public sealed class WordList
    {
        readonly string[] _words;

        WordList(string[] sortedWords) => _words = sortedWords;

        public int Count => _words.Length;

        /// <summary>One word per line; case and surrounding whitespace are ignored.</summary>
        public static WordList Parse(string text)
        {
            var words = new List<string>(160000);
            foreach (var line in text.Split('\n'))
            {
                var w = line.Trim();
                if (w.Length > 0) words.Add(w.ToLowerInvariant());
            }
            var array = words.ToArray();
            if (!IsSorted(array)) Array.Sort(array, StringComparer.Ordinal); // the shipped list is pre-sorted
            return new WordList(array);
        }

        static bool IsSorted(string[] words)
        {
            for (int i = 1; i < words.Length; i++)
                if (string.CompareOrdinal(words[i - 1], words[i]) > 0) return false;
            return true;
        }

        public bool Contains(string word) => Array.BinarySearch(_words, word, StringComparer.Ordinal) >= 0;

        public bool HasPrefix(string prefix)
        {
            int i = Array.BinarySearch(_words, prefix, StringComparer.Ordinal);
            if (i >= 0) return true;
            i = ~i;
            return i < _words.Length && _words[i].StartsWith(prefix, StringComparison.Ordinal);
        }
    }
}
