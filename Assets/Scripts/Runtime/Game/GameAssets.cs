using System;
using SuperOttie.Audio;
using UnityEngine;
using UnityEngine.UIElements;

namespace SuperOttie.Game
{
    [Serializable]
    public sealed class ThemeDefinition
    {
        public string id = "day";
        public Sprite background;
        public Color skyColor = new Color(0.55f, 0.8f, 1f);
        [Tooltip("Multiplied over terrain, blocks and pipes so they sit in the scene's light.")]
        public Color terrainTint = Color.white;
        public AudioClip music;

        [Header("Optional overrides (empty = the shared sprite)")]
        public Sprite tileGrass;
        public Sprite tileDirt;
        public Sprite bush;
        public Sprite flowers;
        public Sprite reeds;
    }

    [Serializable]
    public sealed class SfxClip
    {
        public Sfx id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    /// <summary>
    /// Central catalogue of everything the runtime needs. Filled by the editor setup
    /// (Super Ottie > Setup Project) from the Assets/Art, Assets/Audio and Assets/Levels folders.
    /// </summary>
    [CreateAssetMenu(menuName = "Super Ottie/Game Assets")]
    public sealed class GameAssets : ScriptableObject
    {
        public const string ResourcePath = "GameAssets";

        [Header("Player")]
        public Sprite playerIdle;
        public Sprite[] playerRun = Array.Empty<Sprite>();
        public Sprite playerJump;
        public Sprite playerFall;
        public Sprite playerHurt;
        public Sprite playerWin;

        [Header("Enemies")]
        public Sprite[] crabWalk = Array.Empty<Sprite>();
        public Sprite crabFlat;
        public Sprite[] puffer = Array.Empty<Sprite>();

        [Header("Items and blocks")]
        public Sprite coin;
        public Sprite fish;
        public Sprite blockQuestion;
        public Sprite blockUsed;
        public Sprite blockBrick;
        public Sprite blockStone;

        [Header("Terrain")]
        public Sprite tileGrass;
        public Sprite tileDirt;
        public Sprite pipeTop;
        public Sprite pipeBody;

        [Header("Goal")]
        public Sprite flagPole;
        public Sprite flagBall;
        public Sprite flag;

        [Header("Decor")]
        public Sprite bush;
        public Sprite flowers;
        public Sprite reeds;
        public Sprite sign;

        [Header("World")]
        public ThemeDefinition[] themes = Array.Empty<ThemeDefinition>();
        public TextAsset[] levels = Array.Empty<TextAsset>();

        [Header("UI")]
        public VisualTreeAsset uiLayout;
        public PanelSettings panelSettings;
        public Font font;
        public Sprite logo;
        public Sprite titleArt;
        public Sprite iconLife;
        public Sprite stickBase;
        public Sprite stickKnob;
        public Sprite buttonJump;
        public Sprite buttonPause;

        [Header("Music")]
        public AudioClip musicTitle;
        public AudioClip musicEnding;
        public AudioClip jingleLevelClear;
        public AudioClip jingleGameOver;
        public AudioClip jingleDeath;

        [Header("Sound effects")]
        public SfxClip[] sfx = Array.Empty<SfxClip>();

        public static GameAssets Load()
        {
            var assets = Resources.Load<GameAssets>(ResourcePath);
            if (assets == null) throw new InvalidOperationException($"Missing Resources/{ResourcePath}.asset. Run 'Super Ottie > Setup Project'.");
            return assets;
        }

        public Sprite GroundTopFor(ThemeDefinition theme) => Or(theme.tileGrass, tileGrass);
        public Sprite GroundFillFor(ThemeDefinition theme) => Or(theme.tileDirt, tileDirt);
        public Sprite BushFor(ThemeDefinition theme) => Or(theme.bush, bush);
        public Sprite FlowersFor(ThemeDefinition theme) => Or(theme.flowers, flowers);
        public Sprite ReedsFor(ThemeDefinition theme) => Or(theme.reeds, reeds);

        static Sprite Or(Sprite themed, Sprite shared) => themed != null ? themed : shared;

        public ThemeDefinition GetTheme(string id)
        {
            foreach (var t in themes)
                if (string.Equals(t.id, id, StringComparison.OrdinalIgnoreCase)) return t;
            return themes.Length > 0 ? themes[0] : new ThemeDefinition();
        }
    }
}
