# Ultimate Drive: installation

Ultimate Drive is fully offline. No account, internet connection or extra downloads are needed to play.

## Android
Requirements: Android phone or tablet, 64-bit (ARM64), landscape orientation.
1. Copy `UltimateDrive.apk` to the device (USB cable, or any file transfer).
2. Open it from the Files app. If Android asks, allow "Install unknown apps" for that app, then tap Install.
3. Start Ultimate Drive. The game picks a graphics preset for your device. You can change it in the Settings screen.
4. If it runs slowly or the device gets hot: in Settings choose the Low graphics preset and a 30 FPS frame cap.

## Windows
Requirements: Windows 10 or 11, 64-bit, a graphics card that supports DirectX 11.
1. Unzip `UltimateDrive_Windows.zip` to a folder (do not run it from inside the zip).
2. Open the folder and run `UltimateDrive.exe`. Keep all files in the folder together.
3. If Windows SmartScreen warns about an unknown publisher, choose More info > Run anyway (the game is not code-signed).
4. Change resolution, window mode and graphics in Settings. Press F3 to show an FPS counter.

## Controls
| | Windows | Android |
|---|---|---|
| Accelerate / brake | W / S or arrows, gamepad triggers | On-screen pedals |
| Steer | A / D or arrows, gamepad stick | Steering wheel, left/right buttons, or tilt (Settings) |
| Handbrake | Space | Handbrake button |
| Nitro | Shift | Nitro button |
| Camera | C | Camera button |
| Reset car | R | Not on the touch HUD |
| Pause | Esc | Pause button or Back |

## Your save data
Progress (coins, cars, upgrades, missions, best laps, settings) is stored on the device, per driver profile, and never leaves it.
Uninstalling the game on Android removes the save. On Windows, deleting the game folder does not remove it: the save is in `%USERPROFILE%\AppData\LocalLow\<company name>\<product name>\UltimateDrive` (the company and product names come from Player Settings).

## Problems
- Game closes at start on Android: the device is not ARM64, or has too little free memory. Close other apps and try again.
- Low FPS: in Settings choose the Low preset, lower Render Scale, or set the frame cap to 30.
