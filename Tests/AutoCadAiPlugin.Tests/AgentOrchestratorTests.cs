using System.Collections.Generic;
using System.Threading.Tasks;
using AutoCadAiPlugin.AI.Orchestrator;
using AutoCadAiPlugin.AI.Providers;
using AutoCadAiPlugin.Core.AiContracts;
using AutoCadAiPlugin.Core.Enums;
using AutoCadAiPlugin.Core.Interfaces;
using AutoCadAiPlugin.Infrastructure.Logging;
using AutoCadAiPlugin.Tests.Fakes;
using AutoCadAiPlugin.Tools.Base;
using Xunit;

namespace AutoCadAiPlugin.Tests;

public class AgentOrchestratorTests
{
    [Fact]
    public async Task Orchestrator_ExecutesFullLoopAndModifiesCadService()
    {
        var fakeCad = new FakeCadService();
        var registry = new ToolRegistry();
        var logger = new SafeFileLogger();
        var providers = new List<IAiProvider> { new MockAiProvider() };

        var orchestrator = new AgentOrchestrator(providers, registry, fakeCad, logger);
        var conversation = new AiConversation();
        var config = new AiProviderConfig { ProviderType = AiProviderType.Mock, Language = "fa" };

        string reply = await orchestrator.RunConversationTurnAsync(
            conversation,
            "یک مستطیل 200 در 100 بکش، وسط آن یک سوراخ با قطر 40 ایجاد کن و ابعاد اصلی را درج کن",
            config);

        Assert.NotEmpty(reply);
        Assert.NotEmpty(fakeCad.ExecutedOperations);
        Assert.Contains(fakeCad.ExecutedOperations, op => op.Contains("CreateRectangle"));
        Assert.Contains(fakeCad.ExecutedOperations, op => op.Contains("CreateCircle"));
        Assert.Contains(fakeCad.ExecutedOperations, op => op.Contains("CreateLinearDimension"));
    }
}
