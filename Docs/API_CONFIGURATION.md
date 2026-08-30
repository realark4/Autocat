# Autocat AI Provider Configuration & Secret Storage

## 1. Supported Providers & Models

| Provider | Recommended Models | Notes |
|---|---|---|
| **Mock (Offline)** | `mock-agent`, `mock-precision` | No API key or internet required. Perfect for testing. |
| **OpenAI** | `gpt-4o`, `gpt-4o-mini`, `o3-mini` | Requires OpenAI API key (`sk-...`). |
| **Google Gemini** | `gemini-2.0-flash`, `gemini-1.5-pro` | Requires Google AI Studio API key (`AIza...`). |
| **Anthropic** | `claude-3-7-sonnet-20250219`, `claude-3-5-haiku` | Requires Anthropic API key (`sk-ant-...`). |
| **Local / Custom** | `llama3`, `qwen2.5-coder`, etc. | Set Provider to `OpenAI` and enter custom `Base URL` (e.g. `http://localhost:11434/v1`). |

---

## 2. Windows DPAPI Security Architecture

Autocat adheres to enterprise zero-trust secret storage:
- API Keys are encrypted at rest using `System.Security.Cryptography.ProtectedData.Protect` with `DataProtectionScope.CurrentUser`.
- Secret payloads are stored in `%LOCALAPPDATA%\Ark4Studio\Autocat\secrets.dat`.
- Settings JSON (`settings.json`) only retains non-sensitive parameters (Provider, Model, Theme, Language) and **never** writes API keys in plain text.
- The `SafeFileLogger` redacts any detected API key patterns from log files.

---

## 3. Privacy Settings

- **Send Drawing Context** (Default: `ON`): Sends document metadata (e.g. active space, active layer, bounding boxes of selected items) to the AI to enhance spatial reasoning. If switched `OFF`, only the raw user prompt is transmitted.
- **Confirm Destructive Actions** (Default: `ON`): Prompts the user with an interactive Approval Card before executing high-risk operations (e.g. `erase_entity`, mass transforms).
