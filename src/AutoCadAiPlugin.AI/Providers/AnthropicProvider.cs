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

public class AnthropicProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    public AiProviderType ProviderType => AiProviderType.Anthropic;

    public AnthropicProvider(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<AiResponse> SendMessageAsync(
        List<AiMessage> history,
        List<ToolDefinition> availableTools,
        AiProviderConfig config,
        CancellationToken cancellationToken = default)
    {
        string model = string.IsNullOrWhiteSpace(config.Model) ? "claude-3-7-sonnet-20250219" : config.Model;
        string baseUrl = string.IsNullOrWhiteSpace(config.BaseUrl) ? "https://api.anthropic.com/v1" : config.BaseUrl.TrimEnd('/');
        string url = $"{baseUrl}/messages";

        var payload = BuildAnthropicPayload(history, availableTools, config, model);
        string requestJson = JsonSerializer.Serialize(payload);

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("x-api-key", config.ApiKey ?? string.Empty);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return AiResponse.Error($"Anthropic API error ({response.StatusCode}): {responseBody}");
            }

            return ParseAnthropicResponse(responseBody);
        }
        catch (Exception ex)
        {
            return AiResponse.Error($"Anthropic connection error: {ex.Message}");
        }
    }

    public async Task<bool> ValidateConnectionAsync(
        string apiKey,
        string? model,
        string? baseUrl = null,
        CancellationToken cancellationToken = default)
    {
        // Simple 1-token test message
        string baseHost = string.IsNullOrWhiteSpace(baseUrl) ? "https://api.anthropic.com/v1" : baseUrl.TrimEnd('/');
        string url = $"{baseHost}/messages";

        var payload = new
        {
            model = string.IsNullOrWhiteSpace(model) ? "claude-3-5-haiku-20241022" : model,
            max_tokens = 1,
            messages = new[] { new { role = "user", content = "ping" } }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

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

    public Task<List<string>> GetSupportedModelsAsync(
        string apiKey,
        string? baseUrl = null,
        CancellationToken cancellationToken = default)
    {
        var models = new List<string>
        {
            "claude-3-7-sonnet-20250219",
            "claude-3-5-sonnet-20241022",
            "claude-3-5-haiku-20241022",
            "claude-3-opus-20240229"
        };
        return Task.FromResult(models);
    }

    private static object BuildAnthropicPayload(List<AiMessage> history, List<ToolDefinition> availableTools, AiProviderConfig config, string model)
    {
        string systemPrompt = string.Empty;
        var messages = new List<object>();

        foreach (var msg in history)
        {
            if (msg.Role == "system")
            {
                systemPrompt = msg.Content;
                continue;
            }

            if (msg.Role == "tool")
            {
                messages.Add(new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "tool_result",
                            tool_use_id = msg.ToolCallId,
                            content = msg.Content
                        }
                    }
                });
            }
            else if (msg.ToolCalls != null && msg.ToolCalls.Count > 0)
            {
                var contentBlocks = new List<object>();
                if (!string.IsNullOrWhiteSpace(msg.Content))
                {
                    contentBlocks.Add(new { type = "text", text = msg.Content });
                }

                foreach (var tc in msg.ToolCalls)
                {
                    contentBlocks.Add(new
                    {
                        type = "tool_use",
                        id = tc.CallId,
                        name = tc.ToolName,
                        input = tc.Arguments
                    });
                }

                messages.Add(new
                {
                    role = "assistant",
                    content = contentBlocks
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
                name = td.Name,
                description = td.Description,
                input_schema = new
                {
                    type = "object",
                    properties = propsDict,
                    required = td.RequiredProperties
                }
            });
        }

        return new
        {
            model,
            system = string.IsNullOrWhiteSpace(systemPrompt) ? null : systemPrompt,
            messages,
            tools = tools.Count > 0 ? tools : null,
            temperature = config.Temperature,
            max_tokens = config.MaxTokens
        };
    }

    private static AiResponse ParseAnthropicResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("content", out var contentArr) || contentArr.ValueKind != JsonValueKind.Array)
        {
            return AiResponse.Error("Malformed Anthropic response structure.");
        }

        var sb = new StringBuilder();
        var toolCalls = new List<ToolCallRequest>();

        foreach (var block in contentArr.EnumerateArray())
        {
            string type = block.TryGetProperty("type", out var tProp) ? tProp.GetString() ?? string.Empty : string.Empty;

            if (type == "text")
            {
                sb.Append(block.TryGetProperty("text", out var textProp) ? textProp.GetString() : string.Empty);
            }
            else if (type == "tool_use")
            {
                string id = block.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString();
                string name = block.TryGetProperty("name", out var nProp) ? nProp.GetString() ?? string.Empty : string.Empty;

                var argsDict = new Dictionary<string, object?>();
                string argsJson = "{}";

                if (block.TryGetProperty("input", out var inputProp))
                {
                    argsJson = inputProp.GetRawText();
                    try
                    {
                        argsDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(argsJson) ?? new Dictionary<string, object?>();
                    }
                    catch
                    {
                        // Ignore
                    }
                }

                toolCalls.Add(new ToolCallRequest(id, name, argsDict, argsJson));
            }
        }

        return AiResponse.Success(sb.Length > 0 ? sb.ToString() : null, toolCalls);
    }
}
