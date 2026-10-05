using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SuperOttie.Audio;
using SuperOttie.Core;
using SuperOttie.Game;
using SuperOttie.View;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace SuperOttie.Editor
{
    /// <summary>
    /// Idempotent, scriptable project configuration: layers, player settings, the GameAssets catalogue,
    /// UI panel settings and the Main scene. Run from the menu or headless:
    /// <c>Unity -batchmode -executeMethod SuperOttie.Editor.ProjectSetup.RunFromCommandLine</c>.
    /// </summary>
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/Main.unity";
        public const string GameAssetsPath = "Assets/Resources/GameAssets.asset";
        public const string PanelSettingsPath = "Assets/UI/GamePanelSettings.asset";
        public const string BundleId = "com.dind.superottie";
        public const string ProductName = "Super Ottie";

        const string Art = "Assets/Art/";
        const string Audio = "Assets/Audio/";

        static readonly List<string> Problems = new List<string>();

        [MenuItem("Super Ottie/Setup Project")]
        public static void Run()
        {
            Problems.Clear();
            ConfigureLayers();
            ConfigurePlayerSettings();
            ConfigureRenderer();
            AssetDatabase.ImportAsset(Art.TrimEnd('/'), ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(Audio.TrimEnd('/'), ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            var panel = CreatePanelSettings();
            var assets = CreateGameAssets(panel);
            CreateMainScene(assets);
            AssetDatabase.SaveAssets();

            if (Problems.Count > 0) Debug.LogError("[Setup] Problems:\n  " + string.Join("\n  ", Problems));
            else Debug.Log("[Setup] Super Ottie project configured.");
        }

        public static void RunFromCommandLine()
        {
            try
            {
                Run();
                EditorApplication.Exit(Problems.Count == 0 ? 0 : 1);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        // ------------------------------------------------------------------ settings

        static void ConfigureLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            int[] ids = { Layers.Ground, Layers.Player, Layers.Enemy, Layers.Item, Layers.Effects };
            for (int i = 0; i < ids.Length; i++) layers.GetArrayElementAtIndex(ids[i]).stringValue = Layers.Names[i];
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "Din";
            PlayerSettings.productName = ProductName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            PlayerSettings.bundleVersion = "1.0";
            // Each App Store Connect upload needs a higher build number; release scripts pass one in.
            string buildNumber = Environment.GetEnvironmentVariable("OTTIE_BUILD_NUMBER");
            PlayerSettings.iOS.buildNumber = string.IsNullOrWhiteSpace(buildNumber) ? "1" : buildNumber.Trim();

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.statusBarHidden = true;
            PlayerSettings.iOS.requiresFullScreen = true;
            PlayerSettings.iOS.hideHomeButton = true;
            PlayerSettings.iOS.deferSystemGesturesMode = UnityEngine.iOS.SystemGestureDeferMode.All;
            PlayerSettings.iOS.targetOSVersionString = "16.0";
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.iOS.simulatorSdkArchitecture = AppleMobileArchitectureSimulator.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            PlayerSettings.iOS.appleDeveloperTeamID = "WWDLQL8W8W"; // for device builds; the simulator needs no signing
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.runInBackground = false;

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "AppIcon/app_icon.png");
            if (icon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            else Problems.Add("App icon missing: Assets/Art/AppIcon/app_icon.png");
        }

        /// <summary>
        /// The game uses no post-processing. Dropping the renderer's post-process data keeps URP from
        /// loading Bloom/DoF shaders the iOS Simulator GPU can't run (it logs errors every frame).
        /// </summary>
        static void ConfigureRenderer()
        {
            const string rendererPath = "Assets/Settings/Renderer2D.asset";
            var renderer = AssetDatabase.LoadMainAssetAtPath(rendererPath);
            if (renderer == null)
            {
                Problems.Add("Missing " + rendererPath);
                return;
            }
            var so = new SerializedObject(renderer);
            var post = so.FindProperty("m_PostProcessData");
            if (post != null)
            {
                post.objectReferenceValue = null;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ------------------------------------------------------------------ assets

        static PanelSettings CreatePanelSettings()
        {
            var panel = LoadOrCreate<PanelSettings>(PanelSettingsPath);
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/UI/GameTheme.tss");
            if (panel.themeStyleSheet == null) Problems.Add("Missing Assets/UI/GameTheme.tss");
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1920, 1080);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 1f; // scale by height: HUD stays the same size on every landscape aspect
            panel.clearColor = false;
            panel.sortingOrder = 10;
            EditorUtility.SetDirty(panel);
            return panel;
        }

        static GameAssets CreateGameAssets(PanelSettings panel)
        {
            Directory.CreateDirectory("Assets/Resources");
            var a = LoadOrCreate<GameAssets>(GameAssetsPath);
            string sp = Art + "Sprites/";

            a.playerIdle = S(sp + "Player/ottie_idle.png");
            a.playerRun = Enumerable.Range(0, 8).Select(i => sp + $"Player/ottie_run_{i}.png").Where(File.Exists).Select(S).ToArray();
            if (a.playerRun.Length == 0) Problems.Add("No run frames (Sprites/Player/ottie_run_N.png)");
            a.playerJump = S(sp + "Player/ottie_jump.png");
            a.playerFall = S(sp + "Player/ottie_fall.png");
            a.playerHurt = S(sp + "Player/ottie_hurt.png");
            a.playerWin = S(sp + "Player/ottie_win.png");

            a.crabWalk = new[] { S(sp + "Enemies/crab_walk_0.png"), S(sp + "Enemies/crab_walk_1.png") };
            a.crabFlat = S(sp + "Enemies/crab_flat.png");
            a.puffer = new[] { S(sp + "Enemies/puffer_0.png"), S(sp + "Enemies/puffer_1.png") };

            a.coin = S(sp + "Items/coin.png");
            a.fish = S(sp + "Items/fish.png");
            a.blockQuestion = S(sp + "Blocks/block_question.png");
            a.blockUsed = S(sp + "Blocks/block_used.png");
            a.blockBrick = S(sp + "Blocks/block_brick.png");
            a.blockStone = S(sp + "Blocks/block_stone.png");
            a.tileGrass = S(sp + "Tiles/tile_grass.png");
            a.tileDirt = S(sp + "Tiles/tile_dirt.png");
            a.pipeTop = S(sp + "Goal/pipe_top.png");
            a.pipeBody = S(sp + "Goal/pipe_body.png");
            a.flagPole = S(sp + "Goal/flag_pole.png");
            a.flagBall = S(sp + "Goal/flag_ball.png");
            a.flag = S(sp + "Goal/flag.png");
            a.bush = S(sp + "Decor/bush.png");
            a.flowers = S(sp + "Decor/flowers.png");
            a.reeds = S(sp + "Decor/reeds.png");
            a.sign = S(sp + "Decor/sign.png");

            a.themes = new[]
            {
                Theme("day", "bg_day", new Color(0.56f, 0.8f, 1f), "music_level1", Color.white),
                Theme("sunset", "bg_sunset", new Color(1f, 0.66f, 0.5f), "music_level2", new Color(1f, 0.88f, 0.8f)),
                Theme("twilight", "bg_twilight", new Color(0.16f, 0.16f, 0.36f), "music_level3", new Color(0.72f, 0.76f, 0.95f)),
                Theme("autumn", "bg_autumn", new Color(0.98f, 0.86f, 0.68f), "music_level4", new Color(1f, 0.94f, 0.86f), decor: "autumn"),
                Theme("snow", "bg_snow", new Color(0.8f, 0.9f, 1f), "music_level5", new Color(0.95f, 0.97f, 1f), tiles: "snow", decor: "snow"),
                Theme("cave", "bg_cave", new Color(0.1f, 0.08f, 0.2f), "music_level6", new Color(0.86f, 0.84f, 1f), tiles: "cave", decor: "cave"),
            };

            a.levels = Directory.GetFiles("Assets/Levels", "level*.txt").OrderBy(p => p, StringComparer.Ordinal)
                .Select(p => AssetDatabase.LoadAssetAtPath<TextAsset>(p.Replace('\\', '/'))).ToArray();
            if (a.levels.Length == 0) Problems.Add("No levels in Assets/Levels");
            a.wordList = Load<TextAsset>("Assets/Data/words.txt");

            a.uiLayout = Load<VisualTreeAsset>("Assets/UI/Game.uxml");
            a.panelSettings = panel;
            a.font = Load<Font>("Assets/Fonts/LilitaOne-Regular.ttf");
            a.logo = S(Art + "UI/logo.png");
            a.titleArt = S(Art + "UI/title_art.png");
            a.iconLife = S(Art + "UI/icon_life.png");
            a.stickBase = S(Art + "UI/stick_base.png");
            a.stickKnob = S(Art + "UI/stick_knob.png");
            a.buttonJump = S(Art + "UI/btn_jump.png");
            a.buttonPause = S(Art + "UI/btn_pause.png");
            a.iconLock = S(Art + "UI/icon_lock.png");
            a.iconStar = S(Art + "UI/icon_star.png");
            a.iconSoundOn = S(Art + "UI/icon_sound_on.png");
            a.iconSoundOff = S(Art + "UI/icon_sound_off.png");
            a.iconBack = S(Art + "UI/icon_back.png");

            a.musicTitle = Clip("Music/music_title.wav");
            a.musicEnding = Clip("Music/music_ending.wav");
            a.jingleLevelClear = Clip("Music/jingle_level_clear.ogg");
            a.jingleGameOver = Clip("Music/jingle_game_over.ogg");
            a.jingleDeath = Clip("Music/jingle_death.ogg");

            a.sfx = SfxFiles.Select(kv => new SfxClip { id = kv.Key, clip = Clip($"Sfx/sfx_{kv.Value}.wav"), volume = SfxVolume(kv.Key) }).ToArray();

            EditorUtility.SetDirty(a);
            return a;
        }

        static readonly Dictionary<Sfx, string> SfxFiles = new Dictionary<Sfx, string>
        {
            { Sfx.Jump, "jump" }, { Sfx.JumpBig, "jump_big" }, { Sfx.Coin, "coin" }, { Sfx.Stomp, "stomp" },
            { Sfx.Bump, "bump" }, { Sfx.BrickBreak, "brick_break" }, { Sfx.PowerUpAppear, "powerup_appear" },
            { Sfx.PowerUp, "powerup" }, { Sfx.Shrink, "shrink" }, { Sfx.OneUp, "one_up" }, { Sfx.Kick, "kick" },
            { Sfx.Flagpole, "flagpole" }, { Sfx.Pause, "pause" }, { Sfx.Hurry, "hurry" }, { Sfx.UiTap, "ui_tap" },
            { Sfx.Fireworks, "fireworks" },
        };

        static float SfxVolume(Sfx s) => s switch
        {
            Sfx.UiTap => 0.5f,
            Sfx.Jump => 0.7f,
            Sfx.JumpBig => 0.7f,
            _ => 1f,
        };

        /// <param name="tiles">Suffix of theme ground tiles (tile_grass_{tiles}.png, tile_dirt_{tiles}.png), or null for the shared ones.</param>
        /// <param name="decor">Suffix of theme scenery (bush_, flowers_, reeds_{decor}.png), or null for the shared ones.</param>
        static ThemeDefinition Theme(string id, string background, Color sky, string music, Color tint, string tiles = null, string decor = null)
        {
            const string sp = Art + "Sprites/";
            return new ThemeDefinition
            {
                terrainTint = tint,
                id = id,
                background = S(Art + $"Backgrounds/{background}.png"),
                skyColor = sky,
                music = Clip($"Music/{music}.wav"),
                tileGrass = tiles == null ? null : S(sp + $"Tiles/tile_grass_{tiles}.png"),
                tileDirt = tiles == null ? null : S(sp + $"Tiles/tile_dirt_{tiles}.png"),
                bush = decor == null ? null : S(sp + $"Decor/bush_{decor}.png"),
                flowers = decor == null ? null : S(sp + $"Decor/flowers_{decor}.png"),
                reeds = decor == null ? null : S(sp + $"Decor/reeds_{decor}.png"),
            };
        }

        static Sprite S(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Problems.Add("Missing sprite: " + path);
            return sprite;
        }

        static AudioClip Clip(string relative) => Load<AudioClip>(Audio + relative);

        static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) Problems.Add($"Missing {typeof(T).Name}: {path}");
            return asset;
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // ------------------------------------------------------------------ scene

        static void CreateMainScene(GameAssets assets)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.position = new Vector3(0f, PlatformerCamera.DefaultOrthographicSize, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = PlatformerCamera.DefaultOrthographicSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.56f, 0.8f, 1f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;
            camGo.AddComponent<UniversalAdditionalCameraData>();
            camGo.AddComponent<AudioListener>();
            var platformerCamera = camGo.AddComponent<PlatformerCamera>();

            var lightGo = new GameObject("Global Light 2D");
            var light = lightGo.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
            light.color = Color.white;

            var root = new GameObject("Game");
            var audio = root.AddComponent<AudioManager>();
            var doc = root.AddComponent<UIDocument>();
            doc.panelSettings = assets.panelSettings;
            doc.visualTreeAsset = assets.uiLayout;
            var manager = root.AddComponent<GameManager>();
            var so = new SerializedObject(manager);
            so.FindProperty("assets").objectReferenceValue = assets;
            so.FindProperty("document").objectReferenceValue = doc;
            so.FindProperty("audioManager").objectReferenceValue = audio;
            so.FindProperty("gameCamera").objectReferenceValue = platformerCamera;
            so.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            if (File.Exists("Assets/Scenes/SampleScene.unity")) AssetDatabase.DeleteAsset("Assets/Scenes/SampleScene.unity");
        }
    }
}
