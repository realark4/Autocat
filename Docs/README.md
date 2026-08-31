# Autocat - AutoCAD AI Assistant Plugin

**Autocat** is a professional, production-grade AI Assistant plugin for Autodesk AutoCAD that converts natural language instructions (in Persian and English) into native AutoCAD drawing and editing operations via strict, validated Tool/Function Calling.

---

## 🌟 Key Features

- 🤖 **Autonomous Multi-Turn CAD Agent**: Recursively plans, executes, inspects drawing status, and refines drafting operations.
- 🌐 **Multi-Provider Support**: Seamless support for **OpenAI / OpenAI-compatible gateways** (GPT-4o, o3-mini, OpenRouter, Ollama, LM Studio), **Google Gemini**, **Anthropic**, and Mock mode. OpenAI-compatible Base URL and model IDs can be entered manually.
- 🧪 **Deterministic Mock Provider**: Test and demo complex CAD scenarios completely offline without API keys or internet connection.
- 📐 **30+ Native CAD Tools**: Full support for Line, Circle, Arc, Polyline, Rectangle, Ellipse, Move, Copy, Rotate, Scale, Mirror, Fillet, Offset, Trim, Extend, Erase, Linear/Aligned/Radial/Diametric Dimensions, Text/MText, and View/Zoom controls.
- 🔒 **Zero-Trust Security & DPAPI Vault**: API keys are securely encrypted using the Windows Data Protection API (DPAPI) and never leak to plain text files, logs, or history.
- 🎨 **Modern WPF Dockable Panel**: Built with MVVM and CommunityToolkit.Mvvm, featuring Dark/Light themes, Persian RTL and English LTR support, live tool status cards, and user approval modals for destructive actions.
- ⚡ **AutoCAD Threading & Safe Transactions**: Native `DocumentLock` and atomic transactions ensure drawing integrity and instant single-step Undo support.
- 📦 **Official Autoloader Bundle**: Ships with `Autocat.bundle` supporting AutoCAD 2024 (.NET Framework 4.8) and AutoCAD 2025/2026 (.NET 8.0).

---

## 🚀 Quick Start

1. **Copy Bundle**: Copy `Autocat.bundle` folder to `%APPDATA%\Autodesk\ApplicationPlugins\` or use `NETLOAD` on `AutoCadAiPlugin.dll`.
2. **Open AutoCAD**: Type `AICAD` in the AutoCAD command line.
3. **Configure Provider**: Click `⚙️ Settings` (or type `AICADSETTINGS`), select your AI Provider (or choose `Mock`), enter your API key, and click **Save Settings**.
4. **Start Drafting**: Type your command in Persian or English, for example:
   - *«یک مستطیل 200 در 100 بکش، وسط آن یک سوراخ با قطر 40 ایجاد کن و ابعاد را اضافه کن.»*

---

## 📚 Documentation Index

- [Architecture Design & Threading Model](ARCHITECTURE.md)
- [Installation & Deployment Guide](INSTALLATION.md)
- [API Configuration & Security Vault](API_CONFIGURATION.md)
- [Complete CAD Tools Reference & Schemas](TOOLS.md)
