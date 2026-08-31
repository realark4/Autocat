# Autocat - AutoCAD AI Assistant Plugin

**Autocat** is a professional, production-grade AI Assistant plugin for Autodesk AutoCAD that converts natural language instructions (in Persian and English) into native AutoCAD drawing and editing operations via strict, validated Tool/Function Calling.

---

## 🌟 Key Features

- 🤖 **Autonomous Multi-Turn CAD Agent**: Recursively plans, executes, inspects drawing status, and refines drafting operations.
- 🌐 **Multi-Provider Support**: Seamless support for **OpenAI** (GPT-4o, o3-mini), **Google Gemini** (Gemini 2.0 Flash, 1.5 Pro), **Anthropic** (Claude 3.7 Sonnet), and custom endpoints (Ollama, LM Studio).
- 🧪 **Deterministic Mock Provider**: Test and demo complex CAD scenarios completely offline without API keys or internet connection.
- 📐 **30+ Native CAD Tools**: Full support for Line, Circle, Arc, Polyline, Rectangle, Ellipse, Move, Copy, Rotate, Scale, Mirror, Fillet, Offset, Trim, Extend, Erase, Linear/Aligned/Radial/Diametric Dimensions, Text/MText, and View/Zoom controls.
- 🔒 **Zero-Trust Security & DPAPI Vault**: API keys are securely encrypted using the Windows Data Protection API (DPAPI) and never leak to plain text files, logs, or history.
- 🎨 **Modern WPF Dockable Panel**: Built with MVVM and CommunityToolkit.Mvvm, featuring Dark/Light themes, Persian RTL and English LTR support, live tool status cards, user approval modals for destructive actions, and editable OpenAI-compatible gateway/model settings.
- ⚡ **AutoCAD Threading & Safe Transactions**: Native `DocumentLock` and atomic transactions ensure drawing integrity and instant single-step Undo support.
- 📦 **Official Autoloader Bundle**: Ships with `Autocat.bundle` supporting AutoCAD 2024 (.NET Framework 4.8) and AutoCAD 2025/2026 (.NET 8.0).

---

## 🚀 Quick Start

1. **Install Bundle**: Copy the `Autocat.bundle` directory to:
   ```
   %APPDATA%\Autodesk\ApplicationPlugins\
   ```
   *(or use `NETLOAD` inside AutoCAD on `AutoCadAiPlugin.dll`)*
2. **Open Assistant Panel**: Type `AICAD` in AutoCAD command line or click the **AI CAD** tab in the AutoCAD Ribbon.
3. **Configure Provider**: Type `AICADSETTINGS` or click the ⚙️ icon to configure your AI Provider & API Key.
4. **Draft Naturally**:
   - «یک مستطیل 200 در 100 بکش، وسط آن یک سوراخ با قطر 40 ایجاد کن و ابعاد اصلی را درج کن.»
   - «این دایره را 50 میلیمتر به سمت راست ببر.»
   - «یک مقطع 500 در 300 با چهار گوشه R20 و سوراخ مرکزی Ø80 بساز.»

---

## 📂 Documentation

- [System Architecture](Docs/ARCHITECTURE.md)
- [Installation Guide](Docs/INSTALLATION.md)
- [API Configuration & Security](Docs/API_CONFIGURATION.md)
- [CAD Tools Reference](Docs/TOOLS.md)
