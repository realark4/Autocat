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
    private const string DefaultBaseUrl = "https://api.openai.com/v1";
    private const string ChatCompletionsPath = "chat/completions";
    private const string ModelsPath = "models";

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
        try
        {
            Uri endpoint = BuildEndpointUri(config.BaseUrl, ChatCompletionsPath);
            var payload = BuildOpenAiPayload(history, availableTools, config);
            string requestJson = JsonSerializer.Serialize(payload);

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            AddBearerToken(request, config.ApiKey);
            request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return AiResponse.Error($"OpenAI-compatible API error ({response.StatusCode}): {responseBody}");
            }

            return ParseOpenAiResponse(responseBody);
        }
        catch (Exception ex)
        {
            return AiResponse.Error($"OpenAI-compatible connection error: {ex.Message}");
        }
    }

    public async Task<bool> ValidateConnectionAsync(
        string apiKey,
        string? model,
        string? baseUrl = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, BuildEndpointUri(baseUrl, ModelsPath));
            AddBearerToken(request, apiKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            // A number of OpenAI-compatible gateways expose chat completions but do
            // not expose /models. Probe the actual configured model in that case.
            if (response.StatusCode != System.Net.HttpStatusCode.NotFound &&
                response.StatusCode != System.Net.HttpStatusCode.MethodNotAllowed &&
                response.StatusCode != System.Net.HttpStatusCode.NotImplemented)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(model))
            {
                return false;
            }

            string selectedModel = model?.Trim() ?? string.Empty;

            using var probeRequest = new HttpRequestMessage(
                HttpMethod.Post,
                BuildEndpointUri(baseUrl, ChatCompletionsPath));
            AddBearerToken(probeRequest, apiKey);
            probeRequest.Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    model = selectedModel,
                    messages = new[] { new { role = "user", content = "ping" } },
                    temperature = 0,
                    max_tokens = 1
                }),
                Encoding.UTF8,
                "application/json");

            using var probeResponse = await _httpClient.SendAsync(probeRequest, cancellationToken);
            return probeResponse.IsSuccessStatusCode;
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

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, BuildEndpointUri(baseUrl, ModelsPath));
            AddBearerToken(request, apiKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                JsonElement modelsArray;
                bool hasModels = doc.RootElement.TryGetProperty("data", out modelsArray) &&
                                 modelsArray.ValueKind == JsonValueKind.Array;
                if (!hasModels)
                {
                    hasModels = doc.RootElement.TryGetProperty("models", out modelsArray) &&
                                modelsArray.ValueKind == JsonValueKind.Array;
                }

                if (hasModels)
                {
                    var models = new List<string>();
                    foreach (var elem in modelsArray.EnumerateArray())
                    {
                        string id = string.Empty;
                        if (elem.ValueKind == JsonValueKind.String)
                        {
                            id = elem.GetString() ?? string.Empty;
                        }
                        else if (elem.ValueKind == JsonValueKind.Object && elem.TryGetProperty("id", out var idProp))
                        {
                            id = idProp.GetString() ?? string.Empty;
                        }
                        else if (elem.ValueKind == JsonValueKind.Object && elem.TryGetProperty("name", out var nameProp))
                        {
                            id = nameProp.GetString() ?? string.Empty;
                        }

                        if (!string.IsNullOrWhiteSpace(id))
                        {
                            models.Add(id);
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

    private static Uri BuildEndpointUri(string? baseUrl, string endpointPath)
    {
        string rawBaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl?.Trim() ?? DefaultBaseUrl;
        if (!Uri.TryCreate(rawBaseUrl, UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("Base URL must be an absolute HTTP or HTTPS URL.");
        }

        string path = baseUri.AbsolutePath.TrimEnd('/');
        path = RemoveKnownEndpointSuffix(path);
        path = $"{path.TrimEnd('/')}/{endpointPath.TrimStart('/')}";

        var builder = new UriBuilder(baseUri)
        {
            Path = path,
            Fragment = string.Empty
        };
        return builder.Uri;
    }

    private static string RemoveKnownEndpointSuffix(string path)
    {
        string[] knownSuffixes = { "/chat/completions", "/models" };
        foreach (string suffix in knownSuffixes)
        {
            if (path.Equals(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            if (path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return path.Substring(0, path.Length - suffix.Length).TrimEnd('/');
            }
        }

        return path;
    }

    private static void AddBearerToken(HttpRequestMessage request, string? apiKey)
    {
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey?.Trim());
        }
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
            model = string.IsNullOrWhiteSpace(config.Model) ? "gpt-4o" : config.Model.Trim(),
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
