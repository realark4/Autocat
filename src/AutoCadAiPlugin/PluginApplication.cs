using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using AutoCadAiPlugin.AI.Orchestrator;
using AutoCadAiPlugin.AI.Providers;
using AutoCadAiPlugin.Cad.Execution;
using AutoCadAiPlugin.Cad.Services;
using AutoCadAiPlugin.Core.Interfaces;
using AutoCadAiPlugin.Infrastructure.Configuration;
using AutoCadAiPlugin.Infrastructure.Logging;
using AutoCadAiPlugin.Infrastructure.Persistence;
using AutoCadAiPlugin.Infrastructure.Security;
using AutoCadAiPlugin.Infrastructure.Units;
using AutoCadAiPlugin.Palette;
using AutoCadAiPlugin.Ribbon;
using AutoCadAiPlugin.Tools.Base;
using AutoCadAiPlugin.UI.ViewModels;
using AutoCadAiPlugin.UI.Views;

[assembly: ExtensionApplication(typeof(AutoCadAiPlugin.PluginApplication))]

namespace AutoCadAiPlugin;

public class PluginApplication : IExtensionApplication
{
    private static ISecureStorage? _secureStorage;
    private static PluginConfigurationManager? _configManager;
    private static ILoggerService? _logger;
    private static ConversationHistoryStore? _historyStore;
    private static IUnitConverter? _unitConverter;
    private static ICadService? _cadService;
    private static IToolRegistry? _toolRegistry;
    private static IAgentOrchestrator? _orchestrator;
    private static ChatViewModel? _chatViewModel;
    private static AiChatView? _chatView;

    public static ChatViewModel? ChatViewModel => _chatViewModel;
    public static AiChatView? ChatView => _chatView;
    public static PluginConfigurationManager? ConfigManager => _configManager;

    public void Initialize()
    {
        try
        {
            CadDispatcher.Initialize();

            _secureStorage = new DpapiSecureStorage();
            _configManager = new PluginConfigurationManager(_secureStorage);
            _logger = new SafeFileLogger();
            _historyStore = new ConversationHistoryStore();
            _unitConverter = new UnitConverter();
            _cadService = new CadService();
            _toolRegistry = new ToolRegistry();

            var providers = new List<IAiProvider>
            {
                new OpenAiProvider(),
                new GeminiProvider(),
                new AnthropicProvider(),
                new MockAiProvider()
            };

            _orchestrator = new AgentOrchestrator(providers, _toolRegistry, _cadService, _logger);

            _chatViewModel = new ChatViewModel(
                _orchestrator,
                _configManager,
                _historyStore,
                () => new SettingsViewModel(_configManager, providers, () => _chatViewModel?.CloseSettings())
            );

            _chatView = new AiChatView
            {
                DataContext = _chatViewModel
            };

            AiCadRibbonBuilder.InitializeRibbon();

            _logger.LogInfo("Autocat AI Assistant plugin initialized successfully.");

            var doc = Application.DocumentManager.MdiActiveDocument;
            doc?.Editor.WriteMessage("\n=======================================================\n[Autocat] AI CAD Assistant loaded successfully.\nType 'AICAD' to open the AI Assistant Panel.\nType 'AICADSETTINGS' to configure AI Providers & API Keys.\n=======================================================\n");
        }
        catch (System.Exception ex)
        {
            _logger?.LogError("Failed to initialize Autocat plugin", ex);
        }
    }

    public void Terminate()
    {
        try
        {
            AiCadPaletteSet.ClosePalette();
            _logger?.LogInfo("Autocat AI Assistant plugin terminated.");
        }
        catch
        {
            // Ignored on AutoCAD exit
        }
    }
}
