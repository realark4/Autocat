using System.Collections.Generic;
using AutoCadAiPlugin.Core.Enums;

namespace AutoCadAiPlugin.Core.ToolContracts;

public class ToolPropertySchema
{
    public string Type { get; set; } = "string";
    public string Description { get; set; } = string.Empty;
    public bool Required { get; set; }
    public object? DefaultValue { get; set; }
    public List<string>? EnumValues { get; set; }
    public double? Minimum { get; set; }
    public double? Maximum { get; set; }
}

public class ToolDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;
    public bool RequiresConfirmation { get; set; }
    public Dictionary<string, ToolPropertySchema> Properties { get; set; } = new();
    public List<string> RequiredProperties { get; set; } = new();

    public static ToolDefinition Create(
        string name,
        string description,
        RiskLevel riskLevel = RiskLevel.Low,
        bool requiresConfirmation = false)
    {
        return new ToolDefinition
        {
            Name = name,
            Description = description,
            RiskLevel = riskLevel,
            RequiresConfirmation = requiresConfirmation
        };
    }

    public ToolDefinition AddProperty(
        string name,
        string type,
        string description,
        bool required = true,
        object? defaultValue = null,
        List<string>? enumValues = null,
        double? minimum = null,
        double? maximum = null)
    {
        Properties[name] = new ToolPropertySchema
        {
            Type = type,
            Description = description,
            Required = required,
            DefaultValue = defaultValue,
            EnumValues = enumValues,
            Minimum = minimum,
            Maximum = maximum
        };

        if (required && !RequiredProperties.Contains(name))
        {
            RequiredProperties.Add(name);
        }

        return this;
    }
}

public record ToolCallRequest(
    string CallId,
    string ToolName,
    Dictionary<string, object?> Arguments,
    string? RawJson = null,
    string? ThoughtSignature = null
);

public record ToolCallResult(
    string CallId,
    string ToolName,
    bool Success,
    object? Data,
    string? Message = null,
    string? ErrorCode = null
)
{
    public static ToolCallResult Ok(string callId, string toolName, object? data, string? message = null)
        => new(callId, toolName, true, data, message ?? "Success");

    public static ToolCallResult Fail(string callId, string toolName, string message, string? errorCode = "EXECUTION_ERROR")
        => new(callId, toolName, false, null, message, errorCode);
}
