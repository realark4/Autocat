using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoCadAiPlugin.Core.AiContracts;
using AutoCadAiPlugin.Core.Enums;
using AutoCadAiPlugin.Core.Interfaces;
using AutoCadAiPlugin.Core.ToolContracts;

namespace AutoCadAiPlugin.AI.Providers;

public class OpenAiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    public AiProviderType ProviderType => AiProviderType.OpenAI;

    public OpenAiProvider(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<AiResponse> SendMessageAsync(
        List<AiMessage> history,
        List<ToolDefinition> availableTools,
        AiProviderConfig config,
        CancellationToken cancellationToken = default)
    {
        string baseUrl = string.IsNullOrWhiteSpace(config.BaseUrl) ? "https://api.openai.com/v1" : config.BaseUrl.TrimEnd('/');
        string url = $"{baseUrl}/chat/completions";

        var payload = BuildOpenAiPayload(history, availableTools, config);
        string requestJson = JsonSerializer.Serialize(payload);

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey ?? string.Empty);
        request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return AiResponse.Error($"OpenAI API error ({response.StatusCode}): {responseBody}");
            }

            return ParseOpenAiResponse(responseBody);
        }
        catch (Exception ex)
        {
            return AiResponse.Error($"OpenAI connection error: {ex.Message}");
        }
    }

    public async Task<bool> ValidateConnectionAsync(
        string apiKey,
        string? model,
        string? baseUrl = null,
        CancellationToken cancellationToken = default)
    {
        string host = string.IsNullOrWhiteSpace(baseUrl) ? "https://api.openai.com/v1" : baseUrl.TrimEnd('/');
        string url = $"{host}/models";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
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
        var defaultModels = new List<string> { "gpt-4o", "gpt-4o-mini", "gpt-4-turbo", "o3-mini", "gpt-3.5-turbo" };

        string host = string.IsNullOrWhiteSpace(baseUrl) ? "https://api.openai.com/v1" : baseUrl.TrimEnd('/');
        string url = $"{host}/models";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var dataArr) && dataArr.ValueKind == JsonValueKind.Array)
                {
                    var models = new List<string>();
                    foreach (var elem in dataArr.EnumerateArray())
                    {
                        if (elem.TryGetProperty("id", out var idProp))
                        {
                            string id = idProp.GetString() ?? string.Empty;
                            if (id.Contains("gpt") || id.Contains("o1") || id.Contains("o3") || id.Contains("claude") || id.Contains("llama") || id.Contains("qwen"))
                            {
                                models.Add(id);
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
            // Fallback to default
        }

        return defaultModels;
    }

    private static object BuildOpenAiPayload(List<AiMessage> history, List<ToolDefinition> availableTools, AiProviderConfig config)
    {
        var messages = new List<object>();

        foreach (var msg in history)
        {
            if (msg.Role == "tool")
            {
                messages.Add(new
                {
                    role = "tool",
                    tool_call_id = msg.ToolCallId ?? Guid.NewGuid().ToString(),
                    content = msg.Content
                });
            }
            else if (msg.ToolCalls != null && msg.ToolCalls.Count > 0)
            {
                var toolCalls = new List<object>();
                foreach (var tc in msg.ToolCalls)
                {
                    toolCalls.Add(new
                    {
                        id = tc.CallId,
                        type = "function",
                        function = new
                        {
                            name = tc.ToolName,
                            arguments = tc.RawJson ?? JsonSerializer.Serialize(tc.Arguments)
                        }
                    });
                }

                messages.Add(new
                {
                    role = "assistant",
                    content = msg.Content,
                    tool_calls = toolCalls
                });
            }
            else
            {
                messages.Add(new
                {
                    role = msg.Role,
                    content = msg.Content
                });
            }
        }

        var tools = new List<object>();
        foreach (var td in availableTools)
        {
            var propsDict = new Dictionary<string, object>();
            foreach (var p in td.Properties)
            {
                var pObj = new Dictionary<string, object>
                {
                    ["type"] = p.Value.Type.ToLowerInvariant(),
                    ["description"] = p.Value.Description
                };
                if (p.Value.Type.Equals("array", StringComparison.OrdinalIgnoreCase))
                {
                    pObj["items"] = new Dictionary<string, object> { ["type"] = "string" };
                }
                if (p.Value.EnumValues != null) pObj["enum"] = p.Value.EnumValues;
                propsDict[p.Key] = pObj;
            }

            tools.Add(new
            {
                type = "function",
                function = new
                {
                    name = td.Name,
                    description = td.Description,
                    parameters = new
                    {
                        type = "object",
                        properties = propsDict,
                        required = td.RequiredProperties
                    }
                }
            });
        }

        return new
        {
            model = string.IsNullOrWhiteSpace(config.Model) ? "gpt-4o" : config.Model,
            messages,
            tools = tools.Count > 0 ? tools : null,
            temperature = config.Temperature,
            max_tokens = config.MaxTokens
        };
    }

    private static AiResponse ParseOpenAiResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
        {
            return AiResponse.Error("Empty choices returned from OpenAI.");
        }

        var firstChoice = choices[0];
        if (!firstChoice.TryGetProperty("message", out var message))
        {
            return AiResponse.Error("No message found in choice.");
        }

        string? contentText = message.TryGetProperty("content", out var cProp) && cProp.ValueKind == JsonValueKind.String
            ? cProp.GetString()
            : null;

        var toolCallsList = new List<ToolCallRequest>();

        if (message.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.ValueKind == JsonValueKind.Array)
        {
            foreach (var tcElem in toolCalls.EnumerateArray())
            {
                string callId = tcElem.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString();

                if (tcElem.TryGetProperty("function", out var fnElem))
                {
                    string toolName = fnElem.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? string.Empty : string.Empty;
                    string argsJson = fnElem.TryGetProperty("arguments", out var argsProp) ? argsProp.GetString() ?? "{}" : "{}";

                    var argsDict = new Dictionary<string, object?>();
                    try
                    {
                        argsDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(argsJson) ?? new Dictionary<string, object?>();
                    }
                    catch
                    {
                        // JSON parsing fallback
                    }

                    toolCallsList.Add(new ToolCallRequest(callId, toolName, argsDict, argsJson));
                }
            }
        }

        return AiResponse.Success(contentText, toolCallsList);
    }
}
