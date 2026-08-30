using System;
using System.Collections.Generic;
using AutoCadAiPlugin.Core.Enums;
using AutoCadAiPlugin.Core.ToolContracts;

namespace AutoCadAiPlugin.Core.AiContracts;

public class AiMessage
{
    public string Role { get; set; } = "user"; // "system", "user", "assistant", "tool"
    public string Content { get; set; } = string.Empty;
    public List<ToolCallRequest>? ToolCalls { get; set; }
    public string? ToolCallId { get; set; }
    public string? ToolName { get; set; }
    public bool IsError { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static AiMessage User(string content) => new() { Role = "user", Content = content };
    public static AiMessage Assistant(string content) => new() { Role = "assistant", Content = content };
    public static AiMessage System(string content) => new() { Role = "system", Content = content };
    public static AiMessage AssistantToolCalls(List<ToolCallRequest> toolCalls, string? content = null)
        => new() { Role = "assistant", Content = content ?? string.Empty, ToolCalls = toolCalls };
    public static AiMessage ToolResult(string toolCallId, string toolName, string content, bool isError = false)
        => new() { Role = "tool", ToolCallId = toolCallId, ToolName = toolName, Content = content, IsError = isError };
}

public class AiConversation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "New Chat";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<AiMessage> Messages { get; set; } = new();
}

public class AiProviderConfig
{
    public AiProviderType ProviderType { get; set; } = AiProviderType.Mock;
    public string Model { get; set; } = "mock-agent";
    public string? ApiKey { get; set; }
    public string? BaseUrl { get; set; }
    public double Temperature { get; set; } = 0.2;
    public int MaxTokens { get; set; } = 2048;
    public bool SendDrawingContext { get; set; } = true;
    public bool RequireConfirmationForDestructiveOps { get; set; } = true;
    public string Language { get; set; } = "fa"; // "fa" or "en"
    public string Theme { get; set; } = "Dark";
}

public class AiResponse
{
    public string? ContentText { get; set; }
    public List<ToolCallRequest> ToolCalls { get; set; } = new();
    public bool IsFinished { get; set; } = true;
    public string? ErrorMessage { get; set; }

    public static AiResponse Success(string? text, List<ToolCallRequest>? toolCalls = null) => new()
    {
        ContentText = text,
        ToolCalls = toolCalls ?? new List<ToolCallRequest>()
    };

    public static AiResponse Error(string error) => new()
    {
        ErrorMessage = error,
        IsFinished = true
    };
}
