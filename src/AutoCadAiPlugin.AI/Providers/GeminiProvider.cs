using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoCadAiPlugin.Core.AiContracts;
using AutoCadAiPlugin.Core.Enums;
using AutoCadAiPlugin.Core.Interfaces;
using AutoCadAiPlugin.Core.ToolContracts;

namespace AutoCadAiPlugin.AI.Providers;

public class GeminiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    public AiProviderType ProviderType => AiProviderType.Gemini;

    public GeminiProvider(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<AiResponse> SendMessageAsync(
        List<AiMessage> history,
        List<ToolDefinition> availableTools,
        AiProviderConfig config,
        CancellationToken cancellationToken = default)
    {
        string model = config.Model ?? string.Empty;
        if (string.IsNullOrWhiteSpace(model) || 
            model.StartsWith("gemini-2.", StringComparison.OrdinalIgnoreCase) || 
            model.StartsWith("gemini-1.", StringComparison.OrdinalIgnoreCase) ||
            model.Equals("gemini-flash", StringComparison.OrdinalIgnoreCase))
        {
            model = "gemini-3.1-flash-lite";
        }
        string baseUrl = string.IsNullOrWhiteSpace(config.BaseUrl) ? "https://generativelanguage.googleapis.com/v1beta" : config.BaseUrl.TrimEnd('/');
        string url = $"{baseUrl}/models/{model}:generateContent?key={config.ApiKey}";

        var payload = BuildGeminiPayload(history, availableTools, config);
        string requestJson = JsonSerializer.Serialize(payload);

        int maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                string responseBody = await response.Content.ReadAsStringAsync();

                if ((response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable || (int)response.StatusCode == 429) && attempt < maxRetries)
                {
                    await Task.Delay(1500 * attempt, cancellationToken);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    return AiResponse.Error($"Gemini API error ({response.StatusCode}): {responseBody}");
                }

                return ParseGeminiResponse(responseBody);
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                await Task.Delay(1000 * attempt, cancellationToken);
            }
            catch (Exception ex)
            {
                return AiResponse.Error($"Gemini connection error: {ex.Message}");
            }
        }

        return AiResponse.Error("Gemini request failed after retries.");
    }

    public async Task<bool> ValidateConnectionAsync(
        string apiKey,
        string? model,
        string? baseUrl = null,
        CancellationToken cancellationToken = default)
    {
        string baseHost = string.IsNullOrWhiteSpace(baseUrl) ? "https://generativelanguage.googleapis.com/v1beta" : baseUrl.TrimEnd('/');
        string url = $"{baseHost}/models?key={apiKey}";

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<string>> GetSupportedModelsAsync(
        string apiKey,
        string? baseUrl = null,
        CancellationToken cancellationToken = default)
    {
        var defaultModels = new List<string>
        {
            "gemini-2.0-flash",
            "gemini-2.0-flash-lite-preview",
            "gemini-1.5-pro",
            "gemini-1.5-flash",
            "gemini-2.5-pro",
            "gemini-2.5-flash"
        };

        if (string.IsNullOrWhiteSpace(apiKey)) return defaultModels;

        string baseHost = string.IsNullOrWhiteSpace(baseUrl) ? "https://generativelanguage.googleapis.com/v1beta" : baseUrl.TrimEnd('/');
        string url = $"{baseHost}/models?key={apiKey}";

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("models", out var modelsArr))
                {
                    var models = new List<string>();
                    foreach (var elem in modelsArr.EnumerateArray())
                    {
                        if (elem.TryGetProperty("name", out var nameProp))
                        {
                            string rawName = nameProp.GetString() ?? string.Empty;
                            string cleanName = rawName.Replace("models/", string.Empty);
                            if (cleanName.Contains("gemini"))
                            {
                                models.Add(cleanName);
                            }
                        }
                    }
                    if (models.Count > 0)
                    {
                        models.Sort();
                        return models;
                    }
                }
            }
        }
        catch
        {
            // Fallback
        }

        return defaultModels;
    }

    private static object BuildGeminiPayload(List<AiMessage> history, List<ToolDefinition> availableTools, AiProviderConfig config)
    {
        var contents = new List<object>();

        foreach (var msg in history)
        {
            if (msg.Role == "system") continue; // Handled in system_instruction

            if (msg.Role == "tool")
            {
                contents.Add(new
                {
                    role = "user",
                    parts = new object[]
                    {
                        new
                        {
                            functionResponse = new
                            {
                                name = msg.ToolName ?? "cad_tool",
                                response = new
                                {
                                    content = msg.Content
                                }
                            }
                        }
                    }
                });
            }
            else if (msg.ToolCalls != null && msg.ToolCalls.Count > 0)
            {
                var parts = new List<object>();
                if (!string.IsNullOrWhiteSpace(msg.Content))
                {
                    parts.Add(new { text = msg.Content });
                }

                foreach (var tc in msg.ToolCalls)
                {
                    var fnPart = new Dictionary<string, object?>
                    {
                        ["functionCall"] = new
                        {
                            name = tc.ToolName,
                            args = tc.Arguments
                        }
                    };
                    if (!string.IsNullOrEmpty(tc.ThoughtSignature))
                    {
                        // Gemini's REST JSON field is camelCase. The API error refers to
                        // this value as thought_signature, but sending that snake_case key
                        // causes it to be ignored and breaks the next function-calling step.
                        fnPart["thoughtSignature"] = tc.ThoughtSignature;
                    }
                    parts.Add(fnPart);
                }

                contents.Add(new
                {
                    role = "model",
                    parts
                });
            }
            else
            {
                contents.Add(new
                {
                    role = msg.Role == "assistant" ? "model" : "user",
                    parts = new object[]
                    {
                        new { text = msg.Content }
                    }
                });
            }
        }

        var funcDecls = new List<object>();
        foreach (var td in availableTools)
        {
            var propsDict = new Dictionary<string, object>();
            foreach (var p in td.Properties)
            {
                var pObj = new Dictionary<string, object>
                {
                    ["type"] = p.Value.Type.ToUpperInvariant(),
                    ["description"] = p.Value.Description
                };
                if (p.Value.Type.Equals("array", StringComparison.OrdinalIgnoreCase))
                {
                    pObj["items"] = new Dictionary<string, object> { ["type"] = "STRING" };
                }
                if (p.Value.EnumValues != null) pObj["enum"] = p.Value.EnumValues;
                propsDict[p.Key] = pObj;
            }

            funcDecls.Add(new
            {
                name = td.Name,
                description = td.Description,
                parameters = new
                {
                    type = "OBJECT",
                    properties = propsDict,
                    required = td.RequiredProperties
                }
            });
        }

        var systemMsg = history.Find(m => m.Role == "system");
        object? systemInstruction = systemMsg != null
            ? new { parts = new object[] { new { text = systemMsg.Content } } }
            : null;

        return new
        {
            system_instruction = systemInstruction,
            contents,
            tools = funcDecls.Count > 0 ? new object[] { new { functionDeclarations = funcDecls } } : null,
            generationConfig = new
            {
                temperature = config.Temperature,
                maxOutputTokens = config.MaxTokens,
                thinkingConfig = new
                {
                    thinkingBudget = 0
                }
            }
        };
    }

    private static AiResponse ParseGeminiResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
        {
            return AiResponse.Error("No candidates in Gemini response.");
        }

        var firstCandidate = candidates[0];
        if (!firstCandidate.TryGetProperty("content", out var content) || !content.TryGetProperty("parts", out var parts))
        {
            return AiResponse.Error("Malformed Gemini content parts.");
        }

        var sb = new StringBuilder();
        var toolCalls = new List<ToolCallRequest>();

        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var textProp))
            {
                sb.Append(textProp.GetString());
            }

            if (part.TryGetProperty("functionCall", out var fnCall))
            {
                string fnName = fnCall.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? string.Empty : string.Empty;
                var argsDict = new Dictionary<string, object?>();
                string argsJson = "{}";

                if (fnCall.TryGetProperty("args", out var argsProp))
                {
                    argsJson = argsProp.GetRawText();
                    try
                    {
                        argsDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(argsJson) ?? new Dictionary<string, object?>();
                    }
                    catch
                    {
                        // Ignore
                    }
                }

                string? thoughtSig = null;
                if (part.TryGetProperty("thoughtSignature", out var sigProp) ||
                    part.TryGetProperty("thought_signature", out sigProp))
                {
                    thoughtSig = sigProp.GetString();
                }

                toolCalls.Add(new ToolCallRequest(Guid.NewGuid().ToString(), fnName, argsDict, argsJson, thoughtSig));
            }
        }

        return AiResponse.Success(sb.Length > 0 ? sb.ToString() : null, toolCalls);
    }
}
