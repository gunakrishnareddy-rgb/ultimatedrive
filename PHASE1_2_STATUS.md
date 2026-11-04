# ULTIMATE DRIVE - Phase 1 + 2 build (prototype)

DRIVE BEYOND LIMITS. EXPERIENCE THE FREEDOM.

## What is in this package
Everything is created in code at runtime: no art files, no audio files, no licences to track. Scenes contain a single bootstrap object each.

| Area | Script(s) | What it does |
|---|---|---|
| Boot + title | `UI/BootLoader`, `Core/GameBranding` | Splash with the title and tagline, loads the menu |
| Main menu | `UI/MainMenuController` | Title, tagline, rotating 3D car showcase, 3-car chooser, Free Drive, Settings, Quit. Career / Race Modes / Garage are shown disabled as COMING SOON |
| Car physics | `Vehicle/VehicleController`, `CarSettings` | 4 WheelColliders, torque curve engine, auto gearbox (6 speed + reverse), Ackermann steering, ABS, traction control, handbrake slide, anti-roll, drag + downforce, nitro, flip recovery |
| Car body | `Vehicle/ProceduralCar` | Sports car built from primitives, 3 cars: Apex GT (RWD), Vortex RS (AWD), Stallion V8 (RWD) |
| Effects | `Vehicle/VehicleEffects` | Skid marks, tyre smoke, brake lights (scaled by graphics preset) |
| Sound | `Audio/VehicleAudio`, `MasterVolume` | Generated engine (RPM pitch), tyre screech, wind, crash thump. No music yet |
| Camera | `CameraSystem/ChaseCamera` | Chase (drift lean, speed pull-back, FOV, scenery avoidance), hood, bumper. C cycles |
| PC controls | `InputSystemLayer/DesktopDriveInput` | Keyboard + gamepad. Works with Input System, Both, or old Input Manager |
| Android controls | `InputSystemLayer/MobileTouchHud` | Steering wheel, gas, brake, handbrake, nitro, camera, reset, pause. Safe-area aware |
| Platform switch | `PlatformControlManager` | Touch on Android/iOS, keyboard on PC, gamepad hides touch HUD |
| World | `World/PrototypeDriveBuilder`, `TrackBuilder` | ~1.6 km circuit (road + kerb meshes), grass, trees, hills, city blocks, ramps, cones, branded start gantry |
| Progression | `World/LapTimer`, `SaveSystem/ProgressStore` | Lap timing (shortcut-proof), best lap, coins for laps and distance, atomic local save with backup |
| Graphics | `Settings/GraphicsSettingsLite` | Auto / Low / Medium / Ultra, device recommendation, FPS cap on mobile |
| HUD + pause | `UI/DriveHud`, `UI/PauseMenu` | Speed, gear, RPM, nitro, lap, coins; pause with restart, graphics, volume, main menu |
| Editor | `Editor/ProjectSetup` | Creates the 3 scenes, Build Settings order, landscape, IL2CPP, ARM64, no INTERNET permission, keeps runtime shaders in builds |

## Setup (about 5 minutes)
1. Unity Hub > New project > **Unity 6 LTS** > **Universal 3D** template.
2. Package Manager: install **Input System** (accept the restart; "Both" or "Input System Package" both work).
3. Copy this package's `Assets/` folder into the project. Wait for the compile.
4. Scenes are created automatically (Boot, MainMenu, PrototypeDrive). If not: menu **UltimateDrive > Setup > Create Scenes and Apply Project Settings**.
5. Open `Boot` and press Play. Or open `PrototypeDrive` and press Play to drive immediately.
6. Test the phone HUD in the editor: **UltimateDrive > Test > Touch Controls In Editor**, then Play.

## Controls
Windows: W/S gas + brake/reverse, A/D steer, Space handbrake, Shift nitro, C camera, R reset, Esc pause.
Gamepad: RT/LT, left stick, A handbrake, B nitro, Y camera, X reset, Start pause.
Android: on-screen wheel (left), pedals + handbrake + nitro (right), CAM / RESET / pause (top right). Back button pauses.

## Status board (honest)
Nothing here has been compiled or run in Unity yet: it was written without access to the Unity Editor.
Expect to fix a few compile typos on first import, and to tune the driving feel on a real machine.

| Part | Compiles | Windows tested | Android tested |
|---|---|---|---|
| Car physics + prototype | Not tested | Not tested | Not tested |
| Touch HUD | Not tested | Not tested | Not tested |
| Menu / HUD / pause | Not tested | Not tested | Not tested |
| Lap timer + save | Not tested | Not tested | Not tested |

First things to check when you press Play: car does not sink or jitter, accelerates, brakes, reverses, steers, handbrake slides, does not flip at speed, camera follows smoothly, 60 FPS.
Tuning knobs (all in `CarSettings`): spring/damper, forward/sideways stiffness, peak torque, handbrake slide factor, steer falloff.

## Not built yet (next, in this order)
1. Tune and verify Phase 1 on a real machine (needs your feedback).
2. Traffic + lights + AI racers (README Part 7), then racing modes (street, highway, drift, time trial, off-road, parking).
3. Full garage (paint, wheels, upgrades) and Career missions.
4. Day/night and weather, music, streaming open world beyond the single circuit.
5. Final art, achievements, performance pass on real phones, APK/EXE builds with `UltimateDrive > Build`.

## Known limitations
- The world is one circuit with an infield plaza, not a streamed open world.
- Cars are blockout primitives, not realistic models.
- No gyro steering, no racing-wheel force feedback, no music.
- The Android APK and Windows EXE can only be produced by Unity on your machine.

## Web / GitHub / Vercel
See the repository `README.md` (sections 2 and 3). The WebGL build adds: browser touch detection (`Core/PlatformInfo` + `Plugins/WebGL/UltimateDriveWeb.jslib`), save flushing to browser storage, Quit hidden in browsers, and a one-click build menu (`Editor/WebBuilder`). WebGL compression is set to Disabled so Vercel can compress on the fly.
