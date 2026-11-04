# ULTIMATE DRIVE

**DRIVE BEYOND LIMITS. EXPERIENCE THE FREEDOM.**

Offline 3D driving and racing game for Android, Windows and the browser, made with Unity 6 (URP).
Phase 1 + 2 prototype: drivable sports car, touch + keyboard + gamepad controls, circuit, lap timer, 3-car chooser, generated sound, graphics presets, offline save.

> Status: written without access to the Unity Editor. It has **not been compiled or play-tested yet**. See `Docs/PHASE1_2_STATUS.md`.

## 1. Get the project running in Unity (once)
1. Unity Hub > New project > **Unity 6 LTS** > **Universal 3D**. Name it `UltimateDrive`.
2. Package Manager: install **Input System** (restart when asked).
3. Close Unity. Copy this repository's contents (`Assets/`, `web/`, `WebTemplate/`, `Docs/`, `.github/`, `vercel.json`, `.vercelignore`, `.gitignore`, `.gitattributes`, `README.md`) **into that project folder**, merging `Assets/`.
4. Open the project. Scenes (`Boot`, `MainMenu`, `PrototypeDrive`) and settings are created automatically. If not: menu **UltimateDrive > Setup > Create Scenes and Apply Project Settings**.
5. Press Play on `Boot`. Fix any compile errors first (send them to me and I will fix them).

Your Unity project folder is now the Git repository root: it contains `Assets/`, `Packages/`, `ProjectSettings/` (from Unity) plus the files above.

## 2. Put it on GitHub
```bash
cd UltimateDrive
git init
git add .
git commit -m "Ultimate Drive prototype"
git branch -M main
git remote add origin https://github.com/<you>/ultimate-drive.git
git push -u origin main
```
Commit `.meta` files and never commit `Library/` (the `.gitignore` handles it).

## 3. Deploy to Vercel

### Route A (recommended): build locally, Vercel serves the `web/` folder
1. In Unity install the **Web Build Support** module (Unity Hub > Installs > your editor > Add modules).
2. Menu **UltimateDrive > Build > WebGL (to web/ folder for Vercel)**. Wait for the build (first one is slow).
3. `git add web && git commit -m "WebGL build" && git push`.
4. vercel.com > **Add New > Project** > import the GitHub repo. Leave Framework = *Other*. `vercel.json` already sets the output folder to `web`. Click **Deploy**.
5. Every time you rebuild and push `web/`, Vercel redeploys.

If `web/Build/*.wasm` or `*.data` is over GitHub's 100 MB file limit, use Git LFS (`git lfs install`; `.gitattributes` already tracks them) or use Route B.

### Route B (optional): GitHub Actions builds and deploys for you
Needs a Unity licence in CI (GameCI). Follow https://game.ci/docs/github/activation to get `UNITY_LICENSE`, then add these repository secrets (Settings > Secrets and variables > Actions):

| Secret | Where from |
|---|---|
| `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` | GameCI activation guide |
| `VERCEL_TOKEN` | Vercel > Account Settings > Tokens |
| `VERCEL_ORG_ID`, `VERCEL_PROJECT_ID` | run `npx vercel link` once locally, then read `.vercel/project.json` |

Push to `main` and `.github/workflows/webgl-vercel.yml` builds the game and deploys it. If you use Route B, do not also connect the repo to Vercel's Git integration (double deploys).

`WebTemplate/index.html` is the branded page (loading bar, fullscreen, controls help, rotate hint, error messages). The WebGL build menu copies it over Unity's default `index.html` automatically, so edit that file, not the one in `web/`.

## Web version: what to expect
- Keyboard and gamepad work. Phones get the touch controls automatically (the page reports a coarse pointer).
- Click the game once so it gets keyboard focus and audio starts.
- Mobile browsers are slow for 3D; the Android app is the better phone route.
- Progress is saved in the browser's storage. Clearing site data erases it.
- First load downloads the whole game (about 20-40 MB), then it is cached.

## Android and Windows builds
Menu **UltimateDrive > Build** (from the release package): Android APK/AAB and Windows 64-bit. These need the matching Unity modules.

## Controls
Windows: W/S gas + brake/reverse, A/D steer, Space handbrake, Shift nitro, C camera, R reset, Esc pause. Gamepad: RT/LT, left stick, A handbrake, B nitro, Y camera, X reset, Start pause. Touch: on-screen wheel and pedals.
