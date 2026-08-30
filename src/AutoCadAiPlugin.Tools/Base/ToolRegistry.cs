using System;
using System.Collections.Generic;
using System.Linq;
using AutoCadAiPlugin.Core.Interfaces;
using AutoCadAiPlugin.Core.ToolContracts;
using AutoCadAiPlugin.Tools.Implementations;

namespace AutoCadAiPlugin.Tools.Base;

public class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, ITool> _tools = new(StringComparer.OrdinalIgnoreCase);

    public ToolRegistry()
    {
        RegisterDefaultTools();
    }

    public void Register(ITool tool)
    {
        _tools[tool.Definition.Name] = tool;
    }

    public ITool? GetTool(string name)
    {
        _tools.TryGetValue(name, out var tool);
        return tool;
    }

    public IReadOnlyList<ITool> GetAllTools() => _tools.Values.ToList();

    public List<ToolDefinition> GetToolDefinitions() => _tools.Values.Select(t => t.Definition).ToList();

    private void RegisterDefaultTools()
    {
        // Drawing info
        Register(new GetDrawingInfoTool());
        Register(new GetActiveLayerTool());
        Register(new GetLayersTool());
        Register(new CreateLayerTool());
        Register(new SetCurrentLayerTool());
        Register(new GetLinearUnitsTool());

        // Selection & Inspection
        Register(new GetSelectedEntitiesTool());
        Register(new SelectEntitiesTool());
        Register(new GetEntityInfoTool());
        Register(new GetBoundingBoxTool());
        Register(new GetEntitiesInWindowTool());
        Register(new GetEntitiesByLayerTool());
        Register(new GetEntitiesByTypeTool());

        // Geometry
        Register(new CreateLineTool());
        Register(new CreateCircleTool());
        Register(new CreateArcTool());
        Register(new CreatePolylineTool());
        Register(new CreateRectangleTool());
        Register(new CreateEllipseTool());

        // Modifications
        Register(new MoveEntityTool());
        Register(new CopyEntityTool());
        Register(new RotateEntityTool());
        Register(new ScaleEntityTool());
        Register(new MirrorEntityTool());
        Register(new EraseEntityTool());
        Register(new FilletEntityTool());
        Register(new OffsetEntityTool());
        Register(new TrimEntityTool());
        Register(new ExtendEntityTool());

        // Dimensions
        Register(new CreateLinearDimensionTool());
        Register(new CreateAlignedDimensionTool());
        Register(new CreateRadiusDimensionTool());
        Register(new CreateDiameterDimensionTool());
        Register(new CreateAngularDimensionTool());

        // Text & Views
        Register(new CreateTextTool());
        Register(new CreateMTextTool());
        Register(new ZoomExtentsTool());
        Register(new ZoomEntityTool());
        Register(new ZoomWindowTool());
    }
}
