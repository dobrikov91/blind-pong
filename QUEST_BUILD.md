# Quest Build — Phase 0

## Prerequisites (one time)

1. **Android Build Support** — in Unity Hub → Installs → your Unity 6 version → Add Modules →
   tick *Android Build Support* (includes Android SDK & NDK Tools + OpenJDK).

2. **Developer Mode on the headset** — Meta Horizon app on your phone →
   Devices → your headset → Developer Mode ON.
   Connect the headset via USB; accept the "Allow USB debugging" prompt inside it.

---

## Unity Editor steps

### 1. Switch build target
**File → Build Settings → Android → Switch Platform**
(takes a minute to reimport assets)

### 2. Enable OpenXR
**Project Settings → XR Plug-in Management**
- Install if prompted
- Android tab → tick **OpenXR**

**Project Settings → XR Plug-in Management → OpenXR** (Android tab)
- Click **+** under *Feature Sets* → add **Meta Quest Feature Set**
- Under *Interaction Profiles* click **+** → add **Meta Quest Touch Pro Controller Profile**
  (covers Quest 2, 3, and Pro controllers)
- Fix any validation errors shown in the OpenXR panel (usually just clicking the Fix button)

### 3. Player settings (Android tab)
**Project Settings → Player → Android**

| Setting | Value |
|---|---|
| Graphics API | Vulkan only (remove OpenGL ES 3) |
| Scripting Backend | IL2CPP |
| Target Architecture | ARM64 only |
| Minimum API Level | 29 |
| Install Location | Automatic |

### 4. Set vrMode in the scene
Open your Phase 0 scene, select the Bootstrap GameObject,
tick **Vr Mode** in the Inspector — this disables the desktop fly-camera.
*(The bootstrap also auto-detects XR at runtime as a fallback.)*

---

## Build & deploy

**File → Build Settings → Build And Run**
with the Quest connected via USB.

Unity builds the APK and pushes it directly to the headset.
The app appears in the headset's App Library under **Unknown Sources**.

---

## What to expect in VR

- You are standing at the center of the arena (head at 0,0,0)
- The ball spawns above and bounces off all six walls
- Head movement is tracked — turn your head to localize the ball by ear
- Debug visuals (green walls, orange ball) are still on; turn off `debugVisuals`
  in the Inspector for the real blind experience

---

## Troubleshooting

| Symptom | Fix |
|---|---|
| Build fails — SDK not found | Unity Hub → Add Modules → Android Build Support |
| "No OpenXR runtime" at launch | Make sure Meta Quest app is set as the active OpenXR runtime (on PC for PCVR, or irrelevant for standalone Quest) |
| App installs but crashes | Check `adb logcat` — most common cause is missing ARM64 library |
| Audio works but no HRTF / sounds flat | Project Settings → Audio → Spatializer Plugin must be **Steam Audio Spatializer**; Steam Audio Android ARM64 binaries are already in `Assets/Plugins/SteamAudio/Binaries/Android/arm64/` |
| Ball audio listener is at wrong position | The AudioListener is on Camera.main; in OpenXR this is driven by the headset, so it should be correct — if not, verify Camera.main resolves to the XR camera |
