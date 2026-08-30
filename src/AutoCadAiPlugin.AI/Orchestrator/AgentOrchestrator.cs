using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoCadAiPlugin.AI.Prompts;
using AutoCadAiPlugin.Core.AiContracts;
using AutoCadAiPlugin.Core.Enums;
using AutoCadAiPlugin.Core.Interfaces;
using AutoCadAiPlugin.Core.ToolContracts;
using AutoCadAiPlugin.Tools.Validation;

namespace AutoCadAiPlugin.AI.Orchestrator;

public class AgentOrchestrator : IAgentOrchestrator
{
    private readonly Dictionary<AiProviderType, IAiProvider> _providers;
    private readonly IToolRegistry _toolRegistry;
    private readonly ICadService _cadService;
    private readonly ILoggerService _logger;
    private const int MaxTurns = 10;

    public event Action<ToolCallRequest, ToolExecutionStatus, string?>? OnToolStatusChanged;
    public event Action<string>? OnStatusMessage;

    public AgentOrchestrator(
        IEnumerable<IAiProvider> providers,
        IToolRegistry toolRegistry,
        ICadService cadService,
        ILoggerService logger)
    {
        _toolRegistry = toolRegistry;
        _cadService = cadService;
        _logger = logger;

        _providers = new Dictionary<AiProviderType, IAiProvider>();
        foreach (var p in providers)
        {
            _providers[p.ProviderType] = p;
        }
    }

    public async Task<string> RunConversationTurnAsync(
        AiConversation conversation,
        string userPrompt,
        AiProviderConfig config,
        Func<ToolCallRequest, Task<bool>>? requestUserApproval = null,
        CancellationToken cancellationToken = default)
    {
        if (!_providers.TryGetValue(config.ProviderType, out var provider))
        {
            throw new InvalidOperationException($"Provider '{config.ProviderType}' is not registered.");
        }

        // Add user prompt to conversation
        conversation.Messages.Add(AiMessage.User(userPrompt));

        // Inject / update system prompt
        string systemPrompt = await SystemPromptBuilder.BuildSystemPromptAsync(config, _cadService);
        var activeHistory = new List<AiMessage> { AiMessage.System(systemPrompt) };
        activeHistory.AddRange(conversation.Messages);

        var availableTools = _toolRegistry.GetToolDefinitions();
        int currentTurn = 0;

        while (currentTurn < MaxTurns && !cancellationToken.IsCancellationRequested)
        {
            currentTurn++;
            OnStatusMessage?.Invoke(config.Language == "fa" ? "در حال تحلیل درخواست توسط هوش مصنوعی..." : "AI analyzing request...");

            var response = await provider.SendMessageAsync(activeHistory, availableTools, config, cancellationToken);

            if (!string.IsNullOrWhiteSpace(response.ErrorMessage))
            {
                string errMsg = $"AI Provider Error: {response.ErrorMessage}";
                _logger.LogError(errMsg);
                conversation.Messages.Add(AiMessage.Assistant(errMsg));
                return errMsg;
            }

            // If no tool calls were requested, we have the final assistant response
            if (response.ToolCalls.Count == 0)
            {
                string finalMsg = response.ContentText ?? (config.Language == "fa" ? "عملیات با موفقیت انجام شد." : "Operation completed.");
                conversation.Messages.Add(AiMessage.Assistant(finalMsg));
                return finalMsg;
            }

            // Assistant requested tool execution(s)
            var assistantMsg = AiMessage.AssistantToolCalls(response.ToolCalls, response.ContentText);
            activeHistory.Add(assistantMsg);
            conversation.Messages.Add(assistantMsg);

            foreach (var toolCall in response.ToolCalls)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var tool = _toolRegistry.GetTool(toolCall.ToolName);
                if (tool == null)
                {
                    string missingMsg = $"Error: Tool '{toolCall.ToolName}' is not recognized or whitelisted.";
                    var failResult = ToolCallResult.Fail(toolCall.CallId, toolCall.ToolName, missingMsg);
                    activeHistory.Add(AiMessage.ToolResult(toolCall.CallId, toolCall.ToolName, missingMsg, true));
                    conversation.Messages.Add(AiMessage.ToolResult(toolCall.CallId, toolCall.ToolName, missingMsg, true));
                    OnToolStatusChanged?.Invoke(toolCall, ToolExecutionStatus.Failed, missingMsg);
                    continue;
                }

                // 1. Parameter validation
                var valResult = ToolParameterValidator.Validate(tool.Definition, toolCall.Arguments);
                if (!valResult.IsValid)
                {
                    string validationError = $"Validation failed: {string.Join("; ", valResult.Errors)}";
                    activeHistory.Add(AiMessage.ToolResult(toolCall.CallId, toolCall.ToolName, validationError, true));
                    conversation.Messages.Add(AiMessage.ToolResult(toolCall.CallId, toolCall.ToolName, validationError, true));
                    OnToolStatusChanged?.Invoke(toolCall, ToolExecutionStatus.Failed, validationError);
                    continue;
                }

                // 2. Interactive user confirmation for destructive / high risk operations
                bool needsConfirm = tool.Definition.RequiresConfirmation ||
                                   (config.RequireConfirmationForDestructiveOps && tool.Definition.RiskLevel >= RiskLevel.High);

                if (needsConfirm && requestUserApproval != null)
                {
                    OnToolStatusChanged?.Invoke(toolCall, ToolExecutionStatus.RequiresConfirmation, "Waiting for user approval...");
                    bool approved = await requestUserApproval(toolCall);
                    if (!approved)
                    {
                        string cancelledMsg = "Tool execution cancelled by user.";
                        activeHistory.Add(AiMessage.ToolResult(toolCall.CallId, toolCall.ToolName, cancelledMsg, true));
                        conversation.Messages.Add(AiMessage.ToolResult(toolCall.CallId, toolCall.ToolName, cancelledMsg, true));
                        OnToolStatusChanged?.Invoke(toolCall, ToolExecutionStatus.Cancelled, cancelledMsg);
                        continue;
                    }
                }

                // 3. Execute Tool in AutoCAD
                OnToolStatusChanged?.Invoke(toolCall, ToolExecutionStatus.Executing, null);
                var sw = Stopwatch.StartNew();
                ToolCallResult result;

                try
                {
                    result = await tool.ExecuteAsync(toolCall.CallId, toolCall.Arguments, _cadService, cancellationToken);
                }
                catch (Exception ex)
                {
                    result = ToolCallResult.Fail(toolCall.CallId, toolCall.ToolName, $"Exception during execution: {ex.Message}");
                    _logger.LogError($"Tool {toolCall.ToolName} execution exception", ex);
                }
                sw.Stop();

                string resultJson = JsonSerializer.Serialize(result.Data ?? new { message = result.Message, success = result.Success });
                _logger.LogToolExecution(toolCall.ToolName, toolCall.RawJson ?? JsonSerializer.Serialize(toolCall.Arguments), result.Success, resultJson, sw.ElapsedMilliseconds);

                var status = result.Success ? ToolExecutionStatus.Completed : ToolExecutionStatus.Failed;
                OnToolStatusChanged?.Invoke(toolCall, status, result.Message);

                activeHistory.Add(AiMessage.ToolResult(toolCall.CallId, toolCall.ToolName, resultJson, !result.Success));
                conversation.Messages.Add(AiMessage.ToolResult(toolCall.CallId, toolCall.ToolName, resultJson, !result.Success));
            }
        }

        string timeoutMsg = config.Language == "fa" ? "عملیات انجام شد." : "Turn completed.";
        return timeoutMsg;
    }
}
