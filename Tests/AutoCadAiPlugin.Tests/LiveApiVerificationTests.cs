using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoCadAiPlugin.AI.Orchestrator;
using AutoCadAiPlugin.AI.Providers;
using AutoCadAiPlugin.Core.AiContracts;
using AutoCadAiPlugin.Core.Enums;
using AutoCadAiPlugin.Core.Interfaces;
using AutoCadAiPlugin.Infrastructure.Configuration;
using AutoCadAiPlugin.Infrastructure.Logging;
using AutoCadAiPlugin.Infrastructure.Security;
using AutoCadAiPlugin.Tests.Fakes;
using AutoCadAiPlugin.Tools.Base;
using Xunit;

namespace AutoCadAiPlugin.Tests;

public class LiveApiVerificationTests
{
    [Fact]
    public async Task LiveTest_GeminiProvider_SchemaAndExecution()
    {
        var secureStorage = new DpapiSecureStorage();
        var configManager = new PluginConfigurationManager(secureStorage);
        var config = await configManager.LoadConfigWithSecretsAsync();

        if (string.IsNullOrWhiteSpace(config.ApiKey) && config.ProviderType != AiProviderType.Mock)
        {
            return;
        }

        var fakeCad = new FakeCadService();
        var registry = new ToolRegistry();
        var logger = new SafeFileLogger();

        var providers = new List<IAiProvider>
        {
            new GeminiProvider(),
            new OpenAiProvider(),
            new AnthropicProvider(),
            new MockAiProvider()
        };

        var orchestrator = new AgentOrchestrator(providers, registry, fakeCad, logger);

        using var httpClient = new System.Net.Http.HttpClient();
        string listUrl = $"https://generativelanguage.googleapis.com/v1beta/models?key={config.ApiKey}";
        var candidateModels = new List<string>();
        try
        {
            var listResp = await httpClient.GetStringAsync(listUrl);
            using var doc = System.Text.Json.JsonDocument.Parse(listResp);
            if (doc.RootElement.TryGetProperty("models", out var modelsArr))
            {
                foreach (var m in modelsArr.EnumerateArray())
                {
                    string mName = m.GetProperty("name").GetString() ?? "";
                    if (mName.StartsWith("models/")) mName = mName.Substring(7);
                    
                    bool supportsGen = false;
                    if (m.TryGetProperty("supportedGenerationMethods", out var genMethods))
                    {
                        foreach (var gm in genMethods.EnumerateArray())
                        {
                            if (gm.GetString() == "generateContent") supportsGen = true;
                        }
                    }
                    if (supportsGen)
                    {
                        candidateModels.Add(mName);
                    }
                }
            }
            Console.WriteLine($"\n=== FOUND {candidateModels.Count} GENERATE_CONTENT MODELS ===\n{string.Join(", ", candidateModels)}\n========================================\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to list models: {ex.Message}");
        }

        var toolDef = registry.GetToolDefinitions();
        var gemini = new GeminiProvider();
        var history = new List<AiMessage> { AiMessage.User("یک مستطیل 200 در 100 بکش") };
        config.Model = "gemini-3.1-flash-lite";

        Console.WriteLine($"\n--- Testing Model: {config.Model} ---");
        var resp = await gemini.SendMessageAsync(history, toolDef, config);
        Console.WriteLine($"Status: Error='{resp.ErrorMessage}', ToolCalls={resp.ToolCalls.Count}, Text='{resp.ContentText}'");
        foreach (var tc in resp.ToolCalls)
        {
            Console.WriteLine($"  -> ToolCall: {tc.ToolName}, Args: {tc.RawJson}");
        }

        Assert.True(string.IsNullOrEmpty(resp.ErrorMessage), $"API returned error: {resp.ErrorMessage}");
        Assert.NotEmpty(resp.ToolCalls);
    }
}
