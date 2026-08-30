using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoCadAiPlugin.Core.Interfaces;
using AutoCadAiPlugin.Core.Models;
using AutoCadAiPlugin.Core.ToolContracts;
using AutoCadAiPlugin.Tools.Base;

namespace AutoCadAiPlugin.Tools.Implementations;

public class GetSelectedEntitiesTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "get_selected_entities",
        "Gets the geometric info, bounding box, handle, and layer of currently selected objects in AutoCAD."
    );

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        var entities = await cadService.GetSelectedEntitiesAsync();
        if (entities.Count == 0)
        {
            return ToolCallResult.Ok(callId, Definition.Name, entities, "No entities are currently selected in the drawing.");
        }

        return ToolCallResult.Ok(callId, Definition.Name, entities, $"Found {entities.Count} selected entity/entities.");
    }
}

public class SelectEntitiesTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "select_entities",
        "Selects entities in AutoCAD drawing by their hexadecimal handles."
    )
    .AddProperty("handles", "array", "List of entity handles to select", required: true);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        var handles = GetStringList(arguments, "handles");
        if (handles.Count == 0)
            return ToolCallResult.Fail(callId, Definition.Name, "handles list is required and cannot be empty.");

        var selected = await cadService.SelectEntitiesAsync(handles);
        return ToolCallResult.Ok(callId, Definition.Name, selected, $"Selected {selected.Count} entity/entities in drawing.");
    }
}

public class GetEntityInfoTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "get_entity_info",
        "Gets detailed information (type, layer, coordinates, bounding box) for a specific entity by its handle."
    )
    .AddProperty("handle", "string", "Hexadecimal handle of the entity", required: true);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        string? handle = GetString(arguments, "handle");
        if (string.IsNullOrWhiteSpace(handle))
            return ToolCallResult.Fail(callId, Definition.Name, "handle is required.");

        var info = await cadService.GetEntityInfoAsync(handle);
        if (info == null)
            return ToolCallResult.Fail(callId, Definition.Name, $"Entity with handle '{handle}' not found.");

        return ToolCallResult.Ok(callId, Definition.Name, info, $"Retrieved info for {info.EntityType} (Handle: {handle}).");
    }
}

public class GetBoundingBoxTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "get_bounding_box",
        "Gets the bounding box and exact center coordinates of an entity by handle."
    )
    .AddProperty("handle", "string", "Hexadecimal handle of the entity", required: true);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        string? handle = GetString(arguments, "handle");
        if (string.IsNullOrWhiteSpace(handle))
            return ToolCallResult.Fail(callId, Definition.Name, "handle is required.");

        var bbox = await cadService.GetBoundingBoxAsync(handle);
        if (bbox == null)
            return ToolCallResult.Fail(callId, Definition.Name, $"Could not compute bounding box for entity '{handle}'.");

        return ToolCallResult.Ok(callId, Definition.Name, new
        {
            BoundingBox = bbox,
            Center = bbox.Center,
            Width = bbox.Width,
            Height = bbox.Height
        }, $"Bounding box center: ({bbox.Center.X:F2}, {bbox.Center.Y:F2}), Width: {bbox.Width:F2}, Height: {bbox.Height:F2}");
    }
}

public class GetEntitiesInWindowTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "get_entities_in_window",
        "Finds all entities located within a rectangular coordinate window."
    )
    .AddProperty("pt1X", "number", "X coordinate of first window corner", required: true)
    .AddProperty("pt1Y", "number", "Y coordinate of first window corner", required: true)
    .AddProperty("pt2X", "number", "X coordinate of second window corner", required: true)
    .AddProperty("pt2Y", "number", "Y coordinate of second window corner", required: true);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        var pt1 = new CadPoint3D(GetDouble(arguments, "pt1X"), GetDouble(arguments, "pt1Y"), 0);
        var pt2 = new CadPoint3D(GetDouble(arguments, "pt2X"), GetDouble(arguments, "pt2Y"), 0);

        var entities = await cadService.GetEntitiesInWindowAsync(pt1, pt2);
        return ToolCallResult.Ok(callId, Definition.Name, entities, $"Found {entities.Count} entities in specified window.");
    }
}

public class GetEntitiesByLayerTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "get_entities_by_layer",
        "Gets all entities residing on a specific layer."
    )
    .AddProperty("layerName", "string", "Layer name to filter entities by", required: true);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        string? layer = GetString(arguments, "layerName");
        if (string.IsNullOrWhiteSpace(layer))
            return ToolCallResult.Fail(callId, Definition.Name, "layerName is required.");

        var entities = await cadService.GetEntitiesByLayerAsync(layer);
        return ToolCallResult.Ok(callId, Definition.Name, entities, $"Found {entities.Count} entities on layer '{layer}'.");
    }
}

public class GetEntitiesByTypeTool : BaseCadTool
{
    public override ToolDefinition Definition => ToolDefinition.Create(
        "get_entities_by_type",
        "Gets all entities of a specific CAD type (e.g. 'Line', 'Circle', 'Polyline', 'Dimension')."
    )
    .AddProperty("entityType", "string", "Entity type name (e.g. 'Line', 'Circle', 'Polyline', 'RotatedDimension')", required: true);

    public override async Task<ToolCallResult> ExecuteAsync(
        string callId,
        Dictionary<string, object?> arguments,
        ICadService cadService,
        CancellationToken cancellationToken = default)
    {
        string? type = GetString(arguments, "entityType");
        if (string.IsNullOrWhiteSpace(type))
            return ToolCallResult.Fail(callId, Definition.Name, "entityType is required.");

        var entities = await cadService.GetEntitiesByTypeAsync(type);
        return ToolCallResult.Ok(callId, Definition.Name, entities, $"Found {entities.Count} {type} entities.");
    }
}
