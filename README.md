# Super Ottie

A small side-scrolling platformer for iPhone (landscape) in the style of the classic Mario games,
starring Ottie, a chubby baby otter with round glasses. Built with Unity 6 (6000.6.3f1, URP 2D).

- 6 courses: Sunny Meadow, Sunset Shore, Twilight Woods, Autumn Grove, Frosty Peaks, Crystal Caverns,
  each with a midway checkpoint, its own backdrop and music (snow and cave have their own ground and scenery)
- Run, variable-height jump (coyote time + jump buffering), stomp crabs, avoid spiky pufferfish
- `?` blocks with coins and the golden fish power-up (big Ottie breaks bricks and survives one hit)
- Coins (100 = extra life), stomp chains, flagpole height bonus, time bonus, saved best score
- Multi-touch thumbstick and jump button laid out inside the device safe area; keyboard/gamepad also work
- Main menu with **New Game** and **Courses**: clearing a course unlocks the next one, and any unlocked course can be
  started from the course select (a fresh run with full lives that carries on through the later courses).
  Progress is saved on the device (`PlayerPrefs`, key `superottie.cleared`)
- Out of lives? Play **Word Hunt** for another life: an 80-second 4x4 letter grid in the style of the iMessage game.
  Drag through touching letters (diagonals count); finding 3 words wins a life and puts Ottie back into the same course
  (from the checkpoint if reached) with the score kept. It can be played every time the lives run out. Words also score
  points (100 / 400 / 800 / 1400 ...). On a keyboard, type a word and press Enter

## Controls

| Action | Touch | Keyboard | Gamepad |
|---|---|---|---|
| Move | thumbstick (keep your thumb down and slide left/right) | arrows / A D | stick / d-pad |
| Jump (hold for higher) | jump button | Space / Up / W / Z | A |
| Pause | pause button | Esc / P | Start |
| Menus | tap a button or course card | arrows / WASD, Enter or Space, Esc to go back | d-pad / stick, A, B to go back |
| Word Hunt | drag across the letters | type the word, Enter (Backspace / Esc to fix) | (touch or keyboard only) |

## Build and run on the iOS Simulator

```bash
./build_sim.sh
```

This exports the Xcode project from Unity (`Builds/iOS-Simulator`), compiles it with `xcodebuild`
for `iphonesimulator`, then installs and launches it on the iPhone 17 Pro (iOS 27) simulator.
Set `SIM=<udid>` to use another simulator.

For a real device, run `Super Ottie > Build > iOS Device Xcode Project` in the editor, open
`Builds/iOS-Device/Unity-iPhone.xcodeproj` and press Run. Automatic signing is set to your team.

## Tests

```bash
./run_tests.sh EditMode   # rules, parser, level lint, movement model, touch zones, camera math,
                          # course unlocks, menu focus, Word Hunt rules, boards, dictionary, tile hit-testing
./run_tests.sh PlayMode   # real physics: jumping, blocks, power-ups, stomps, pits, goal, scene flow,
                          # plus an autopilot that must finish every shipped course
```

Visual QA captures (explicit, so they don't run by default) render into `Logs/Screenshots`:

```bash
/Applications/Unity/Hub/Editor/6000.6.3f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter MenuScreenshotCapture -testResults Logs/shots.xml -logFile Logs/shots.log
```

## Project layout

```
Assets/
  Scripts/Runtime/
    Core/      GameSession, CourseProgress, StompChain, LevelTimer, layers   (pure C#, unit tested)
    WordHunt/  WordList, LetterGrid + GridSolver + GridGenerator, WordHuntRound (pure C#, unit tested)
    Level/     LevelParser, LevelData, LevelLint, LevelBuilder, LevelContext
    Player/    PlatformerMotor (pure movement model), PlayerController, PlayerVisual
    Entities/  blocks, coins, fish power-up, enemies, flagpole, checkpoint, effects
    Input/     DeviceInput (touch + keyboard + gamepad), TouchZones (thumbstick + jump)
    View/      PlatformerCamera, ParallaxBackground
    Audio/     AudioManager
    UI/        GameUI (UI Toolkit: Game.uxml / Game.uss), MenuView (menus), MenuFocus, WordHuntView
    Game/      GameManager (state machine), WordHuntController, GameAssets catalogue, RuntimeSettings
  Scripts/Editor/  ProjectSetup (layers, player settings, assets, scene), BuildScript, import rules
  Levels/     level1-6.txt, plain-text maps (legend in LevelParser.cs)
  Data/       words.txt, the Word Hunt dictionary (built by Tools/WordList/make_wordlist.py)
  Art/ Audio/ Fonts/ UI/
Tools/ArtPipeline/   image generation prompts and the cut-out/slicing pipeline
Tools/WordList/      ENABLE source list, blocklist and the script that builds Assets/Data/words.txt
```

Design notes:

- **Levels are text.** `LevelParser` turns a map like `..?B?..e..[]..F` into data, `LevelLint`
  rejects unfair layouts (pits wider than 4, steps taller than a jump), and `LevelBuilder` creates the
  tilemap and entities. `Tools/ArtPipeline/make_levels.py` is the authoring script.
- **Movement is a pure model.** `PlatformerMotor` computes velocity from input and a grounded flag,
  so jump height, coyote time and buffering are unit-tested without the engine. The controller only
  feeds it physics results.
- **Interactions use explicit overlap queries**, not collision callbacks, so a stomp versus a hit is
  decided the same way every frame.
- **Word Hunt boards are never duds.** Boards are rolled from 16 classic letter dice and solved against the
  dictionary (depth-first search pruned by prefix lookups in a sorted word array); a board needs at least 40 words.
  The dictionary is ENABLE, 3 to 12 letters, minus `Tools/WordList/blocklist.txt` (slurs, sexual terms, profanity).
- **Everything is reproducible.** `Super Ottie > Setup Project` (or `ProjectSetup.RunFromCommandLine`)
  rebuilds the scene, the asset catalogue and the player settings from the files on disk.

## Art pipeline

All characters, props, tiles, backgrounds, the title art, logo and app icon were generated with
ChatGPT Images from the prompts in `Tools/ArtPipeline/PROMPTS.md` and `Tools/ArtPipeline/prompts/` (the first set via
ChatGPT web, later sets through the Codex CLI with `gen.sh <name>`, which uses `gpt-6-sol` unless `CODEX_MODEL` says
otherwise); the raw outputs are kept in `Tools/ArtPipeline/raw/`. Process them with
`Tools/ArtPipeline/.venv/bin/python process.py <name>` (create the venv with numpy, scipy and pillow).
The generator can't output transparency, so each asset is drawn on flat white and `process.py` cuts it
out: flood-fill of the white backdrop from the borders, alpha un-mixing of the anti-aliased fringe,
slicing sheets into frames by connected components, uniform scaling and bottom-centre pivots, and
edge cross-fading so tiles and backgrounds repeat seamlessly.

See [CREDITS.md](CREDITS.md) for music, sound and font licences.
