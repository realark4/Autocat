using System;
using System.Collections.Generic;
using System.Text.Json;
using AutoCadAiPlugin.Core.ToolContracts;

namespace AutoCadAiPlugin.Tools.Validation;

public class ValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; set; } = new();

    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Fail(string error) => new() { IsValid = false, Errors = new List<string> { error } };
    public static ValidationResult Fail(List<string> errors) => new() { IsValid = false, Errors = errors };
}

public static class ToolParameterValidator
{
    public static ValidationResult Validate(ToolDefinition definition, Dictionary<string, object?> arguments)
    {
        var errors = new List<string>();

        foreach (var reqProp in definition.RequiredProperties)
        {
            if (!arguments.ContainsKey(reqProp) || arguments[reqProp] == null)
            {
                // Check if alternative coordinate notation exists (e.g. for center -> centerX/centerY)
                if (reqProp.EndsWith("Point", StringComparison.OrdinalIgnoreCase) || reqProp.Equals("center", StringComparison.OrdinalIgnoreCase))
                {
                    string prefix = reqProp.Equals("center", StringComparison.OrdinalIgnoreCase) ? "center" : reqProp.Substring(0, reqProp.Length - 5);
                    if (arguments.ContainsKey(prefix + "X") || arguments.ContainsKey("x") || arguments.ContainsKey(reqProp + "X"))
                    {
                        continue; // Accepted
                    }
                }
                errors.Add($"Missing required parameter '{reqProp}' for tool '{definition.Name}'.");
            }
        }

        foreach (var kvp in arguments)
        {
            if (kvp.Value == null) continue;

            if (definition.Properties.TryGetValue(kvp.Key, out var schema))
            {
                if (schema.EnumValues != null && schema.EnumValues.Count > 0)
                {
                    string strVal = kvp.Value.ToString() ?? string.Empty;
                    if (!schema.EnumValues.Contains(strVal, StringComparer.OrdinalIgnoreCase))
                    {
                        errors.Add($"Parameter '{kvp.Key}' value '{strVal}' is not in allowed values: [{string.Join(", ", schema.EnumValues)}].");
                    }
                }

                if (schema.Minimum.HasValue || schema.Maximum.HasValue)
                {
                    double numVal = 0.0;
                    bool isNum = false;

                    if (kvp.Value is JsonElement elem && elem.ValueKind == JsonValueKind.Number)
                    {
                        numVal = elem.GetDouble();
                        isNum = true;
                    }
                    else if (double.TryParse(kvp.Value.ToString(), out var parsed))
                    {
                        numVal = parsed;
                        isNum = true;
                    }

                    if (isNum)
                    {
                        if (schema.Minimum.HasValue && numVal < schema.Minimum.Value)
                        {
                            errors.Add($"Parameter '{kvp.Key}' value {numVal} is less than minimum allowed {schema.Minimum.Value}.");
                        }
                        if (schema.Maximum.HasValue && numVal > schema.Maximum.Value)
                        {
                            errors.Add($"Parameter '{kvp.Key}' value {numVal} is greater than maximum allowed {schema.Maximum.Value}.");
                        }
                    }
                }
            }
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Fail(errors);
    }
}
