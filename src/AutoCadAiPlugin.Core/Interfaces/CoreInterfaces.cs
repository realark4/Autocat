using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoCadAiPlugin.Core.AiContracts;
using AutoCadAiPlugin.Core.Enums;
using AutoCadAiPlugin.Core.Models;
using AutoCadAiPlugin.Core.ToolContracts;

namespace AutoCadAiPlugin.Core.Interfaces;

public interface IAiProvider
{
    AiProviderType ProviderType { get; }
    Task<AiResponse> SendMessageAsync(
        List<AiMessage> history,
        List<ToolDefinition> availableTools,
        AiProviderConfig config,
        CancellationToken cancellationToken = default);

    Task<bool> ValidateConnectionAsync(
        string apiKey,
        string? model,
        string? baseUrl = null,
        CancellationToken cancellationToken = default);

    Task<List<string>> GetSupportedModelsAsync(
        string apiKey,
        string? baseUrl = null,
        CancellationToken cancellationToken = default);
}

public interface ICadService
{
    // Drawing queries
    Task<CadDrawingInfo> GetDrawingInfoAsync();
    Task<string> GetActiveLayerAsync();
    Task<List<CadLayerInfo>> GetLayersAsync();
    Task<bool> CreateLayerAsync(string layerName, string? colorName = null);
    Task<bool> SetCurrentLayerAsync(string layerName);
    Task<CadUnitType> GetLinearUnitsAsync();

    // Selection & Entity Queries
    Task<List<CadEntityInfo>> GetSelectedEntitiesAsync();
    Task<List<string>> SelectEntitiesAsync(List<string> handles);
    Task<CadEntityInfo?> GetEntityInfoAsync(string handle);
    Task<CadBoundingBox?> GetBoundingBoxAsync(string handle);
    Task<List<CadEntityInfo>> GetEntitiesInWindowAsync(CadPoint3D pt1, CadPoint3D pt2);
    Task<List<CadEntityInfo>> GetEntitiesByLayerAsync(string layerName);
    Task<List<CadEntityInfo>> GetEntitiesByTypeAsync(string entityType);

    // Entity Creation (Geometry)
    Task<string> CreateLineAsync(CadPoint3D startPoint, CadPoint3D endPoint, string? layer = null);
    Task<string> CreateCircleAsync(CadPoint3D center, double radius, string? layer = null);
    Task<string> CreateArcAsync(CadPoint3D center, double radius, double startAngleDegrees, double endAngleDegrees, string? layer = null);
    Task<string> CreatePolylineAsync(List<CadPoint2D> vertices, bool closed = false, string? layer = null);
    Task<string> CreateRectangleAsync(CadPoint2D corner1, CadPoint2D corner2, double cornerRadius = 0.0, string? layer = null);
    Task<string> CreateEllipseAsync(CadPoint3D center, CadPoint3D majorAxisVector, double radiusRatio, string? layer = null);

    // Entity Modification
    Task<bool> MoveEntityAsync(string handle, CadPoint3D fromPoint, CadPoint3D toPoint);
    Task<string> CopyEntityAsync(string handle, CadPoint3D fromPoint, CadPoint3D toPoint);
    Task<bool> RotateEntityAsync(string handle, CadPoint3D basePoint, double rotationAngleDegrees);
    Task<bool> ScaleEntityAsync(string handle, CadPoint3D basePoint, double scaleFactor);
    Task<string> MirrorEntityAsync(string handle, CadPoint3D axisPt1, CadPoint3D axisPt2, bool eraseSource = false);

    // Editing Operations
    Task<bool> FilletEntityAsync(string handle1, string handle2, double radius);
    Task<string> OffsetEntityAsync(string handle, double distance, CadPoint3D sidePoint);
    Task<bool> TrimEntityAsync(string cuttingEdgeHandle, string entityToTrimHandle, CadPoint3D pickPoint);
    Task<bool> ExtendEntityAsync(string boundaryHandle, string entityToExtendHandle, CadPoint3D pickPoint);

    // Erase
    Task<bool> EraseEntityAsync(string handle);
    Task<int> EraseEntitiesAsync(List<string> handles);

    // Dimensions
    Task<string> CreateLinearDimensionAsync(CadPoint3D pt1, CadPoint3D pt2, CadPoint3D textLocation, bool isHorizontal, string? layer = null);
    Task<string> CreateAlignedDimensionAsync(CadPoint3D pt1, CadPoint3D pt2, CadPoint3D textLocation, string? layer = null);
    Task<string> CreateRadiusDimensionAsync(string circleOrArcHandle, CadPoint3D textLocation, string? layer = null);
    Task<string> CreateDiameterDimensionAsync(string circleOrArcHandle, CadPoint3D textLocation, string? layer = null);
    Task<string> CreateAngularDimensionAsync(CadPoint3D centerPoint, CadPoint3D pt1, CadPoint3D pt2, CadPoint3D arcPoint, string? layer = null);

    // Text & MText
    Task<string> CreateTextAsync(string text, CadPoint3D insertionPoint, double height, double rotationDegrees = 0.0, string? layer = null);
    Task<string> CreateMTextAsync(string text, CadPoint3D insertionPoint, double width, double height, string? layer = null);

    // Views
    Task<bool> ZoomExtentsAsync();
    Task<bool> ZoomEntityAsync(string handle);
    Task<bool> ZoomWindowAsync(CadPoint3D pt1, CadPoint3D pt2);
}

public interface ITool
{
    ToolDefinition Definition { get; }
    Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default);
}

public interface IToolRegistry
{
    void Register(ITool tool);
    ITool? GetTool(string name);
    IReadOnlyList<ITool> GetAllTools();
    List<ToolDefinition> GetToolDefinitions();
}

public interface ISecureStorage
{
    Task SaveSecretAsync(string key, string secret);
    Task<string?> GetSecretAsync(string key);
    Task DeleteSecretAsync(string key);
}

public interface ILoggerService
{
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message, Exception? ex = null);
    void LogToolExecution(string toolName, string argumentsJson, bool success, string? resultOrError, long elapsedMs);
}

public interface IUnitConverter
{
    double ConvertToCadUnits(double value, string unitString, CadUnitType activeDrawingUnit);
    double ConvertFromCadUnits(double value, string targetUnitString, CadUnitType activeDrawingUnit);
    CadUnitType ParseUnit(string unitString);
}

public interface IAgentOrchestrator
{
    event Action<ToolCallRequest, ToolExecutionStatus, string?>? OnToolStatusChanged;
    event Action<string>? OnStatusMessage;

    Task<string> RunConversationTurnAsync(
        AiConversation conversation,
        string userPrompt,
        AiProviderConfig config,
        Func<ToolCallRequest, Task<bool>>? requestUserApproval = null,
        CancellationToken cancellationToken = default);
}
