# Blind Pong VR

A VR ping-pong game with **zero visuals**. Black screen. You locate and hit the ball purely by spatial audio — 3D positional sound via HRTF — and feel it through controller haptics. Designed as both a genuine audio-localization challenge and a comedic experience for spectators watching someone swing at thin air.

## How it works

- A ball bounces around an invisible arena
- Each surface produces a distinct sound: wall click, floor thud, front-wall ping, back-wall thock, paddle pop
- A continuous whoosh tracks the ball's flight between bounces — pitch and volume scale with speed and distance
- The right-hand controller is your paddle
- The controller vibrates as the ball approaches, with intensity rising the closer it gets
- On contact: a sharp haptic burst + the paddle flashes white

## Controls

| Input | Action |
|---|---|
| Move right controller / mouse | Move paddle |
| A button / R key | Respawn ball at paddle position |
| B button / V key | Toggle ball visibility (debug) |

## Setup

1. **Unity Hub** → New Project → Unity 6 LTS → 3D Core
2. Copy this repo's `Assets/`, `Packages/`, `ProjectSettings/` into the project root
3. Package Manager installs Steam Audio from the git URL in `manifest.json`
   - If that fails: download the `.unitypackage` from the Steam Audio releases page and import manually
4. **Project Settings → Audio → Spatializer Plugin → "Steam Audio Spatializer"**
5. Create an empty scene, add an empty GameObject, attach `Phase0Bootstrap`, hit Play
6. Wear headphones. Close your eyes.

**For Quest (via Quest Link or standalone build):** tick `vrMode` on the `Phase0Bootstrap` component, or see `QUEST_BUILD.md`.

## Validation

The core mechanic rests on one assumption: HRTF spatialisation is accurate enough to track a moving ball by sound alone. To verify:

> Wear headphones. Close your eyes. Try to predict where the ball is **before** each click. If you can track it reliably from the click alone — Phase 0 passes.

## Tunable parameters

All parameters are exposed in the Unity Inspector. Change them at runtime without restarting.

### Phase0Bootstrap (scene root)

| Parameter | Default | Description |
|---|---|---|
| `arenaWidth` | 1.5 m | Arena width (X axis) |
| `arenaHeight` | 1.5 m | Arena height (Y axis) |
| `arenaZMin` | −1 m | Near edge of arena (behind player) |
| `arenaZMax` | 5 m | Far edge of arena |
| `ballSoundMode` | WhiteNoise | Continuous ball whoosh: `WhiteNoise`, `PinkNoise`, `PureSine`, or `AudioFile` |
| `customBallSound` | — | AudioClip to use when mode is `AudioFile` (wav/mp3/ogg) |
| `debugVisuals` | true | Shows coloured ball, paddle, floor grid, and dim ambient light |
| `vrMode` | false | Force VR mode (auto-detected if Quest Link is active) |

### BallController (on Ball at runtime)

| Parameter | Default | Description |
|---|---|---|
| `minSpeed` | 0.5 m/s | Ball never drops below this speed |
| `maxSpeed` | 15 m/s | Ball is capped at this speed |
| `gravityScale` | 0.3 | Gravity multiplier (0 = floats, 1 = Earth gravity) |

### BallAudio (on Ball at runtime)

| Parameter | Default | Description |
|---|---|---|
| `whooshMinVolume` | 0.08 | Whoosh volume at minimum ball speed |
| `whooshMaxVolume` | 0.35 | Whoosh volume at maximum ball speed |
| `whooshVolumeScale` | 1× (3× for procedural modes) | Global volume multiplier for the whoosh source |
| `whooshPitchMin` | 0.6× | Playback pitch at minimum ball speed |
| `whooshPitchMax` | 2.0× | Playback pitch at maximum ball speed |
| `whooshPitchModulate` | true | Disable to keep pitch at 1× (set automatically for `AudioFile` mode) |

### Paddle (on Paddle at runtime)

| Parameter | Default | Description |
|---|---|---|
| `velocityMultiplier` | 1.5 | How much paddle swing velocity is added to the ball on hit |
| `spaceInvadersMode` | true | Paddle stays face-forward; hand controls Y and Z only |
| `flashColor` | White | Paddle colour on ball contact |
| `flashDuration` | 0.12 s | How long the flash lasts |
| `hapticMaxDistance` | 2.0 m | Proximity buzz starts at this distance from the ball |
| `hapticMinDistance` | 0.15 m | Full haptic intensity reached at this distance |
| `hapticMaxAmplitude` | 0.6 | Peak haptic amplitude (0–1) during proximity ramp |

## Ball whoosh sound modes

| Mode | Character |
|---|---|
| **WhiteNoise** | Broadband hiss — default, works well at all distances |
| **PinkNoise** | Softer, more natural 1/f noise |
| **PureSine** | Clean tone, pitch-shifts dramatically with ball speed |
| **AudioFile** | Your own clip — plays at natural speed with no pitch warp |

## Tech stack

- **Engine:** Unity 6 LTS
- **XR:** OpenXR + Meta XR SDK, Quest 2/3 target
- **Spatial audio:** Steam Audio (HRTF + room simulation)
- **Physics:** Unity Rigidbody, `ContinuousDynamic` collision detection
- **Input:** Unity Input System (`InputAction` bindings)

## Project phases

| Phase | Status | Description |
|---|---|---|
| 0 | ✅ Done | Audio localization PoC — black scene, bouncing ball, HRTF, paddle |
| 1 | ✅ Done | Unity 6 + OpenXR + Meta XR SDK + Steam Audio spatializer |
| 2 | ✅ Done | Ball physics, arena colliders, paddle with velocity transfer |
| 3 | ⚠️ Mostly done | Per-event sounds, whoosh, proximity haptics — **Steam Audio room simulation (reverb/reflections) not yet configured** |
| 4 | Planned | Game loop — solo rally mode, audio-only UI, training mode with ghost visual |
| 5 | Planned | Multiplayer (high risk, likely post-v1) |
| 6 | Planned | Polish — real sound assets, difficulty scaling, HRTF calibration |
