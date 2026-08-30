using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoCadAiPlugin.Core.Enums;
using AutoCadAiPlugin.Core.Interfaces;
using AutoCadAiPlugin.Core.ToolContracts;
using AutoCadAiPlugin.Tools.Base;

namespace AutoCadAiPlugin.Tools.Implementations;

public class GetDrawingInfoTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "get_drawing_info",
        "Gets metadata about the active AutoCAD drawing including file path, space, active layer, units, and entity count."
    );

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        var info = await cadService.GetDrawingInfoAsync();
        return ToolCallResult.Ok(callId, Definition.Name, info, $"Drawing '{info.DocumentName}' has {info.EntityCount} entities in {info.ActiveSpace}. Active layer is '{info.ActiveLayer}'.");
    }
}

public class GetActiveLayerTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "get_active_layer",
        "Gets the name of the currently active layer in the drawing."
    );

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        var layer = await cadService.GetActiveLayerAsync();
        return ToolCallResult.Ok(callId, Definition.Name, new { ActiveLayer = layer }, $"Active layer is '{layer}'.");
    }
}

public class GetLayersTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "get_layers",
        "Gets the list of all layers defined in the current AutoCAD drawing."
    );

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        var layers = await cadService.GetLayersAsync();
        return ToolCallResult.Ok(callId, Definition.Name, layers, $"Found {layers.Count} layers in drawing.");
    }
}

public class CreateLayerTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "create_layer",
        "Creates a new layer with optional color (e.g. 'Red', 'Green', 'Cyan' or ACI index 1-255)."
    )
    .AddProperty("layerName", "string", "Name of the new layer to create", required: true)
    .AddProperty("color", "string", "Optional layer color (e.g. 'Red', 'Yellow', 'Green', 'Cyan', 'Blue', 'Magenta', 'White' or index 1-7)", required: false);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        string? layerName = GetString(arguments, "layerName");
        if (string.IsNullOrWhiteSpace(layerName))
            return ToolCallResult.Fail(callId, Definition.Name, "layerName is required.");

        string? color = GetString(arguments, "color");
        bool created = await cadService.CreateLayerAsync(layerName, color);
        if (created)
            return ToolCallResult.Ok(callId, Definition.Name, new { Layer = layerName, Color = color }, $"Layer '{layerName}' created successfully.");
        else
            return ToolCallResult.Ok(callId, Definition.Name, new { Layer = layerName }, $"Layer '{layerName}' already exists.");
    }
}

public class SetCurrentLayerTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "set_current_layer",
        "Sets the current active layer in AutoCAD."
    )
    .AddProperty("layerName", "string", "The name of the layer to make active", required: true);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        string? layerName = GetString(arguments, "layerName");
        if (string.IsNullOrWhiteSpace(layerName))
            return ToolCallResult.Fail(callId, Definition.Name, "layerName is required.");

        bool success = await cadService.SetCurrentLayerAsync(layerName);
        if (success)
            return ToolCallResult.Ok(callId, Definition.Name, new { ActiveLayer = layerName }, $"Active layer set to '{layerName}'.");
        else
            return ToolCallResult.Fail(callId, Definition.Name, $"Layer '{layerName}' not found in drawing.");
    }
}

public class GetLinearUnitsTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "get_linear_units",
        "Gets the current drawing unit setting (e.g. Millimeters, Inches, Meters)."
    );

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        var units = await cadService.GetLinearUnitsAsync();
        return ToolCallResult.Ok(callId, Definition.Name, new { Units = units.ToString(), Code = (int)units }, $"Current drawing units: {units}");
    }
}
