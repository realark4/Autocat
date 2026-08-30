using System.Collections.Generic;
using System.Threading.Tasks;
using AutoCadAiPlugin.AI.Providers;
using AutoCadAiPlugin.Core.AiContracts;
using AutoCadAiPlugin.Core.Enums;
using AutoCadAiPlugin.Core.ToolContracts;
using AutoCadAiPlugin.Tools.Base;
using Xunit;

namespace AutoCadAiPlugin.Tests;

public class MockProviderScenarioTests
{
    private readonly MockAiProvider _provider = new();
    private readonly ToolRegistry _registry = new();
    private readonly AiProviderConfig _config = new() { ProviderType = AiProviderType.Mock, Language = "fa" };

    [Fact]
    public async Task Scenario1_CirclePrompt_GeneratesCreateCircleToolCall()
    {
        var history = new List<AiMessage>
        {
            AiMessage.User("یک دایره با شعاع 25 در 100,100 بکش")
        };

        var response = await _provider.SendMessageAsync(history, _registry.GetToolDefinitions(), _config);

        Assert.NotEmpty(response.ToolCalls);
        var tc = response.ToolCalls[0];
        Assert.Equal("create_circle", tc.ToolName);
        Assert.Equal(25.0, double.Parse(tc.Arguments["radius"]!.ToString()!));
        Assert.Equal(100.0, double.Parse(tc.Arguments["centerX"]!.ToString()!));
        Assert.Equal(100.0, double.Parse(tc.Arguments["centerY"]!.ToString()!));
    }

    [Fact]
    public async Task Scenario2_RectanglePrompt_GeneratesCreateRectangleToolCall()
    {
        var history = new List<AiMessage>
        {
            AiMessage.User("یک مستطیل 200 در 100 بکش")
        };

        var response = await _provider.SendMessageAsync(history, _registry.GetToolDefinitions(), _config);

        Assert.NotEmpty(response.ToolCalls);
        var tc = response.ToolCalls[0];
        Assert.Equal("create_rectangle", tc.ToolName);
        Assert.Equal(200.0, double.Parse(tc.Arguments["width"]!.ToString()!));
        Assert.Equal(100.0, double.Parse(tc.Arguments["height"]!.ToString()!));
    }

    [Fact]
    public async Task Scenario3_RectangleWithCenteredHole_GeneratesRectangleAndCircle()
    {
        var history = new List<AiMessage>
        {
            AiMessage.User("یک مستطیل 200 در 100 بکش، وسط آن یک سوراخ با قطر 40 ایجاد کن و ابعاد اصلی را درج کن")
        };

        var response = await _provider.SendMessageAsync(history, _registry.GetToolDefinitions(), _config);

        Assert.True(response.ToolCalls.Count >= 2);
        Assert.Equal("create_rectangle", response.ToolCalls[0].ToolName);
        Assert.Equal("create_circle", response.ToolCalls[1].ToolName);

        // Center should be (100, 50) and radius should be 20
        var circleCall = response.ToolCalls[1];
        Assert.Equal(100.0, double.Parse(circleCall.Arguments["centerX"]!.ToString()!));
        Assert.Equal(50.0, double.Parse(circleCall.Arguments["centerY"]!.ToString()!));
        Assert.Equal(20.0, double.Parse(circleCall.Arguments["radius"]!.ToString()!));
    }

    [Fact]
    public async Task Scenario4_MoveSelectedEntity_GeneratesMoveEntityToolCall()
    {
        var history = new List<AiMessage>
        {
            AiMessage.User("دایره انتخاب‌شده را 50 واحد به راست ببر")
        };

        var response = await _provider.SendMessageAsync(history, _registry.GetToolDefinitions(), _config);

        Assert.NotEmpty(response.ToolCalls);
        var tc = response.ToolCalls[0];
        Assert.Equal("move_entity", tc.ToolName);
        Assert.Equal(50.0, double.Parse(tc.Arguments["toX"]!.ToString()!));
        Assert.Equal(0.0, double.Parse(tc.Arguments["toY"]!.ToString()!));
    }

    [Fact]
    public async Task Scenario5_SectionWithFilletsAndHole_GeneratesCompoundPlan()
    {
        var history = new List<AiMessage>
        {
            AiMessage.User("یک مقطع مستطیلی 500 در 300 ایجاد کن، چهار گوشه را R20 کن و وسط آن یک سوراخ Ø80 ایجاد کن")
        };

        var response = await _provider.SendMessageAsync(history, _registry.GetToolDefinitions(), _config);

        Assert.True(response.ToolCalls.Count >= 2);
        Assert.Equal("create_rectangle", response.ToolCalls[0].ToolName);
        Assert.Equal(20.0, double.Parse(response.ToolCalls[0].Arguments["cornerRadius"]!.ToString()!));

        Assert.Equal("create_circle", response.ToolCalls[1].ToolName);
        Assert.Equal(250.0, double.Parse(response.ToolCalls[1].Arguments["centerX"]!.ToString()!));
        Assert.Equal(150.0, double.Parse(response.ToolCalls[1].Arguments["centerY"]!.ToString()!));
        Assert.Equal(40.0, double.Parse(response.ToolCalls[1].Arguments["radius"]!.ToString()!));
    }
}
