using UnityEngine;

namespace SuperOttie.Core
{
    public interface IHighScoreStore
    {
        int Load();
        void Save(int score);
    }

    public sealed class PlayerPrefsHighScoreStore : IHighScoreStore
    {
        const string Key = "superottie.highscore";

        public int Load() => PlayerPrefs.GetInt(Key, 0);

        public void Save(int score)
        {
            if (score <= Load()) return;
            PlayerPrefs.SetInt(Key, score);
            PlayerPrefs.Save();
        }
    }
}
