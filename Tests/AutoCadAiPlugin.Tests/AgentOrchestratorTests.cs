using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoCadAiPlugin.AI.Orchestrator;
using AutoCadAiPlugin.AI.Providers;
using AutoCadAiPlugin.Core.AiContracts;
using AutoCadAiPlugin.Core.Enums;
using AutoCadAiPlugin.Core.Interfaces;
using AutoCadAiPlugin.Core.ToolContracts;
using AutoCadAiPlugin.Infrastructure.Logging;
using AutoCadAiPlugin.Tests.Fakes;
using AutoCadAiPlugin.Tools.Base;
using Xunit;

namespace AutoCadAiPlugin.Tests;

public class AgentOrchestratorTests
{
    [Fact]
    public async Task GeminiProvider_PreservesThoughtSignatureAcrossFunctionCalls()
    {
        const string signature = "opaque-signature-from-gemini";
        var handler = new SequencedHttpMessageHandler(
            """
            {
              "candidates": [{
                "content": {
                  "parts": [{
                    "functionCall": {
                      "name": "create_rectangle",
                      "args": { "width": 200, "height": 100 }
                    },
                    "thoughtSignature": "opaque-signature-from-gemini"
                  }]
                }
              }]
            }
            """,
            """
            {
              "candidates": [{
                "content": {
                  "parts": [{ "text": "done" }]
                }
              }]
            }
            """);
        var provider = new GeminiProvider(new HttpClient(handler));
        var config = new AiProviderConfig
        {
            ProviderType = AiProviderType.Gemini,
            Model = "gemini-3.1-flash-lite"
        };
        var history = new List<AiMessage> { AiMessage.User("Draw a rectangle") };

        var firstResponse = await provider.SendMessageAsync(history, new List<ToolDefinition>(), config);

        Assert.Single(firstResponse.ToolCalls);
        Assert.Equal(signature, firstResponse.ToolCalls[0].ThoughtSignature);

        history.Add(AiMessage.AssistantToolCalls(firstResponse.ToolCalls));
        history.Add(AiMessage.ToolResult(
            firstResponse.ToolCalls[0].CallId,
            firstResponse.ToolCalls[0].ToolName,
            "{\"success\":true}"));

        var secondResponse = await provider.SendMessageAsync(history, new List<ToolDefinition>(), config);

        Assert.Equal("done", secondResponse.ContentText);
        Assert.Equal(2, handler.RequestBodies.Count);
        using var request = JsonDocument.Parse(handler.RequestBodies[1]);
        var functionCallPart = request.RootElement
            .GetProperty("contents")[1]
            .GetProperty("parts")[0];
        Assert.Equal(signature, functionCallPart.GetProperty("thoughtSignature").GetString());
        Assert.False(functionCallPart.TryGetProperty("thought_signature", out _));
    }

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

    private sealed class SequencedHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<string> _responses;

        public SequencedHttpMessageHandler(params string[] responses)
        {
            _responses = new Queue<string>(responses);
        }

        public List<string> RequestBodies { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestBodies.Add(await request.Content!.ReadAsStringAsync());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responses.Dequeue(), Encoding.UTF8, "application/json")
            };
        }
    }
}
