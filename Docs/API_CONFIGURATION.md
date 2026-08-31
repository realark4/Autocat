# Autocat AI Provider Configuration & Secret Storage

## 1. Supported Providers & Models

| Provider | Recommended Models | Notes |
|---|---|---|
| **Mock (Offline)** | `mock-agent`, `mock-precision` | No API key or internet required. Perfect for testing. |
| **OpenAI / OpenAI-compatible** | `gpt-4o`, `gpt-4o-mini`, `o3-mini`, or any model ID returned by the gateway | Supports a custom `Base URL`, manual model names, local servers and proxy gateways. API key can be empty for local gateways. |
| **Google Gemini** | `gemini-2.0-flash`, `gemini-1.5-pro` | Requires Google AI Studio API key (`AIza...`). |
| **Anthropic** | `claude-3-7-sonnet-20250219`, `claude-3-5-haiku` | Requires Anthropic API key (`sk-ant-...`). |
| **Local / Custom** | `llama3`, `qwen2.5-coder`, `deepseek/deepseek-chat`, etc. | Set Provider to `OpenAI`, enter the gateway `Base URL` (e.g. `http://localhost:11434/v1`) and type the exact model ID manually. |

### OpenAI-compatible gateways and proxies

The OpenAI provider also works with services that expose the OpenAI Chat Completions API. In the settings panel:

1. Select **OpenAI**.
2. Enter the gateway URL up to its API version, normally ending in `/v1`.
3. Enter the exact model ID. You can type a value that is not in the built-in suggestions.
4. Enter the gateway key, or leave it empty for a local server.
5. Use **Load models** to read model IDs from `/models`, then use **Test connection**.

Examples:

| Gateway | Base URL | Model example |
|---|---|---|
| OpenRouter | `https://openrouter.ai/api/v1` | `deepseek/deepseek-chat` |
| Ollama | `http://localhost:11434/v1` | `llama3.2` |
| LM Studio | `http://127.0.0.1:1234/v1` | the loaded model ID |
| vLLM / LiteLLM | `http://localhost:8000/v1` | the server’s model ID |

The URL may also be pasted with `/chat/completions` or `/models`; Autocat normalizes those suffixes, so they are not duplicated.

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
