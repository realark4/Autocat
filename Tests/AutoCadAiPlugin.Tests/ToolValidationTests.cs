using System.Collections.Generic;
using AutoCadAiPlugin.Core.Enums;
using AutoCadAiPlugin.Core.ToolContracts;
using AutoCadAiPlugin.Tools.Implementations;
using AutoCadAiPlugin.Tools.Validation;
using Xunit;

namespace AutoCadAiPlugin.Tests;

public class ToolValidationTests
{
    [Fact]
    public void CreateLineTool_Validation_SucceedsWithAllRequiredParameters()
    {
        var tool = new CreateLineTool();
        var args = new Dictionary<string, object?>
        {
            ["startX"] = 0.0,
            ["startY"] = 0.0,
            ["endX"] = 100.0,
            ["endY"] = 200.0
        };

        var result = ToolParameterValidator.Validate(tool.Definition, args);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateLineTool_Validation_FailsWhenMissingRequiredParameter()
    {
        var tool = new CreateLineTool();
        var args = new Dictionary<string, object?>
        {
            ["startX"] = 0.0,
            ["startY"] = 0.0
            // Missing endX, endY
        };

        var result = ToolParameterValidator.Validate(tool.Definition, args);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("endX"));
    }

    [Fact]
    public void ScaleEntityTool_Validation_EnforcesMinimumValue()
    {
        var tool = new ScaleEntityTool();
        var args = new Dictionary<string, object?>
        {
            ["handle"] = "100A",
            ["basePointX"] = 0.0,
            ["basePointY"] = 0.0,
            ["scaleFactor"] = -1.5 // Invalid scale
        };

        var result = ToolParameterValidator.Validate(tool.Definition, args);
        Assert.False(result.IsValid);
    }
}
