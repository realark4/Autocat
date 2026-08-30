# Autocat Installation & Deployment Guide

## System Requirements

- **Operating System**: Windows 10 or Windows 11 (64-bit)
- **Supported AutoCAD Versions**:
  - **AutoCAD 2024** (Runs on .NET Framework 4.8)
  - **AutoCAD 2025** (Runs on .NET 8.0)
  - **AutoCAD 2026** (Runs on .NET 8.0)

---

## Method 1: Standard Autoloader Bundle (Recommended)

1. Locate the compiled `Autocat.bundle` directory in the repository root.
2. Copy the entire `Autocat.bundle` folder to your AutoCAD Application Plugins directory:
   ```
   %APPDATA%\Autodesk\ApplicationPlugins\
   ```
   *(e.g. `C:\Users\<YourUsername>\AppData\Roaming\Autodesk\ApplicationPlugins\Autocat.bundle`)*
   or system-wide:
   ```
   C:\Program Files\Autodesk\ApplicationPlugins\Autocat.bundle
   ```
3. Launch AutoCAD. The plugin will auto-load automatically on startup.
4. Click the **AI CAD** tab in the AutoCAD Ribbon, or type `AICAD` in the command line.

---

## Method 2: Manual NETLOAD

1. Open AutoCAD.
2. Type `NETLOAD` in the command line and press Enter.
3. Browse and select `AutoCadAiPlugin.dll`:
   - For **AutoCAD 2024**: `Autocat.bundle/Contents/2024/AutoCadAiPlugin.dll`
   - For **AutoCAD 2025/2026**: `Autocat.bundle/Contents/2025/AutoCadAiPlugin.dll`
4. Type `AICAD` to open the Assistant panel.

---

## Available Commands

| Command | Description |
|---|---|
| `AICAD` | Toggles the AI Assistant dockable panel. |
| `AICADSETTINGS` | Opens the AI Provider and API Key settings panel directly. |
| `AICADMOCK` | Runs an offline test scenario (Rectangle + Hole + Dimensions). |
