# Super Ottie

A small side-scrolling platformer for iPhone (landscape) in the style of the classic Mario games,
starring Ottie, a chubby baby otter with round glasses. Built with Unity 6 (6000.6.3f1, URP 2D).

- 3 courses: Sunny Meadow, Sunset Shore, Twilight Woods, each with a midway checkpoint
- Run, variable-height jump (coyote time + jump buffering), stomp crabs, avoid spiky pufferfish
- `?` blocks with coins and the golden fish power-up (big Ottie breaks bricks and survives one hit)
- Coins (100 = extra life), stomp chains, flagpole height bonus, time bonus, saved best score
- Multi-touch on-screen controls laid out inside the device safe area; keyboard/gamepad also work

## Controls

| Action | Touch | Keyboard | Gamepad |
|---|---|---|---|
| Move | left/right buttons (slide between them) | arrows / A D | stick / d-pad |
| Jump (hold for higher) | jump button | Space / Up / W / Z | A |
| Pause | pause button | Esc / P | Start |

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
./run_tests.sh EditMode   # rules, parser, level lint, movement model, touch zones, camera math
./run_tests.sh PlayMode   # real physics: jumping, blocks, power-ups, stomps, pits, goal, scene flow,
                          # plus an autopilot that must finish every shipped course
```

## Project layout

```
Assets/
  Scripts/Runtime/
    Core/      GameSession, StompChain, LevelTimer, layers   (pure C#, unit tested)
    Level/     LevelParser, LevelData, LevelLint, LevelBuilder, LevelContext
    Player/    PlatformerMotor (pure movement model), PlayerController, PlayerVisual
    Entities/  blocks, coins, fish power-up, enemies, flagpole, checkpoint, effects
    Input/     DeviceInput (touch + keyboard + gamepad), TouchZones
    View/      PlatformerCamera, ParallaxBackground
    Audio/     AudioManager
    UI/        GameUI (UI Toolkit: Game.uxml / Game.uss)
    Game/      GameManager (state machine), GameAssets catalogue, RuntimeSettings
  Scripts/Editor/  ProjectSetup (layers, player settings, assets, scene), BuildScript, import rules
  Levels/     level1-3.txt, plain-text maps (legend in LevelParser.cs)
  Art/ Audio/ Fonts/ UI/
Tools/ArtPipeline/   image generation prompts and the cut-out/slicing pipeline
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
- **Everything is reproducible.** `Super Ottie > Setup Project` (or `ProjectSetup.RunFromCommandLine`)
  rebuilds the scene, the asset catalogue and the player settings from the files on disk.

## Art pipeline

All characters, props, tiles, backgrounds, the title art, logo and app icon were generated with
ChatGPT Images from the prompts in `Tools/ArtPipeline/PROMPTS.md` (via ChatGPT web, since the Codex
CLI route in `gen.sh` was down at the time); the raw outputs are kept in `Tools/ArtPipeline/raw/`.
The generator can't output transparency, so each asset is drawn on flat white and `process.py` cuts it
out: flood-fill of the white backdrop from the borders, alpha un-mixing of the anti-aliased fringe,
slicing sheets into frames by connected components, uniform scaling and bottom-centre pivots, and
edge cross-fading so tiles and backgrounds repeat seamlessly.

See [CREDITS.md](CREDITS.md) for music, sound and font licences.
