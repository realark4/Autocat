using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoCadAiPlugin.Core.Enums;
using AutoCadAiPlugin.Core.Interfaces;
using AutoCadAiPlugin.Core.Models;

namespace AutoCadAiPlugin.Tests.Fakes;

public class FakeCadService : ICadService
{
    public List<string> ExecutedOperations { get; } = new();
    public Dictionary<string, CadEntityInfo> Entities { get; } = new();
    private int _handleCounter = 1000;

    private string NextHandle() => (++_handleCounter).ToString("X");

    public Task<CadDrawingInfo> GetDrawingInfoAsync()
    {
        return Task.FromResult(new CadDrawingInfo(
            "TestDrawing.dwg",
            "C:\\Drawings\\TestDrawing.dwg",
            "ModelSpace",
            "0",
            "World",
            CadUnitType.Millimeters,
            Entities.Count,
            new List<string> { "0", "Defpoints", "Dimensions" }
        ));
    }

    public Task<string> GetActiveLayerAsync() => Task.FromResult("0");

    public Task<List<CadLayerInfo>> GetLayersAsync() => Task.FromResult(new List<CadLayerInfo>
    {
        new("0", "7", false, false, false),
        new("Dimensions", "1", false, false, false)
    });

    public Task<bool> CreateLayerAsync(string layerName, string? colorName = null)
    {
        ExecutedOperations.Add($"CreateLayer: {layerName}");
        return Task.FromResult(true);
    }

    public Task<bool> SetCurrentLayerAsync(string layerName)
    {
        ExecutedOperations.Add($"SetCurrentLayer: {layerName}");
        return Task.FromResult(true);
    }

    public Task<CadUnitType> GetLinearUnitsAsync() => Task.FromResult(CadUnitType.Millimeters);

    public Task<List<CadEntityInfo>> GetSelectedEntitiesAsync()
    {
        var list = new List<CadEntityInfo>(Entities.Values);
        return Task.FromResult(list);
    }

    public Task<List<string>> SelectEntitiesAsync(List<string> handles) => Task.FromResult(handles);

    public Task<CadEntityInfo?> GetEntityInfoAsync(string handle)
    {
        Entities.TryGetValue(handle, out var ent);
        return Task.FromResult(ent);
    }

    public Task<CadBoundingBox?> GetBoundingBoxAsync(string handle)
    {
        if (Entities.TryGetValue(handle, out var ent)) return Task.FromResult(ent.BoundingBox);
        return Task.FromResult<CadBoundingBox?>(new CadBoundingBox(new CadPoint3D(0, 0, 0), new CadPoint3D(200, 100, 0)));
    }

    public Task<List<CadEntityInfo>> GetEntitiesInWindowAsync(CadPoint3D pt1, CadPoint3D pt2) => Task.FromResult(new List<CadEntityInfo>(Entities.Values));

    public Task<List<CadEntityInfo>> GetEntitiesByLayerAsync(string layerName) => Task.FromResult(new List<CadEntityInfo>(Entities.Values));

    public Task<List<CadEntityInfo>> GetEntitiesByTypeAsync(string entityType) => Task.FromResult(new List<CadEntityInfo>(Entities.Values));

    public Task<string> CreateLineAsync(CadPoint3D startPoint, CadPoint3D endPoint, string? layer = null)
    {
        string h = NextHandle();
        ExecutedOperations.Add($"CreateLine: ({startPoint.X},{startPoint.Y}) -> ({endPoint.X},{endPoint.Y})");
        Entities[h] = new CadEntityInfo(h, "100", "Line", layer ?? "0", "7", new CadBoundingBox(startPoint, endPoint));
        return Task.FromResult(h);
    }

    public Task<string> CreateCircleAsync(CadPoint3D center, double radius, string? layer = null)
    {
        string h = NextHandle();
        ExecutedOperations.Add($"CreateCircle: Center=({center.X},{center.Y}), R={radius}");
        var minPt = new CadPoint3D(center.X - radius, center.Y - radius, 0);
        var maxPt = new CadPoint3D(center.X + radius, center.Y + radius, 0);
        Entities[h] = new CadEntityInfo(h, "101", "Circle", layer ?? "0", "7", new CadBoundingBox(minPt, maxPt));
        return Task.FromResult(h);
    }

    public Task<string> CreateArcAsync(CadPoint3D center, double radius, double startAngleDegrees, double endAngleDegrees, string? layer = null)
    {
        string h = NextHandle();
        ExecutedOperations.Add($"CreateArc: Center=({center.X},{center.Y}), R={radius}, Angles={startAngleDegrees}-{endAngleDegrees}");
        return Task.FromResult(h);
    }

    public Task<string> CreatePolylineAsync(List<CadPoint2D> vertices, bool closed = false, string? layer = null)
    {
        string h = NextHandle();
        ExecutedOperations.Add($"CreatePolyline: Vertices={vertices.Count}, Closed={closed}");
        return Task.FromResult(h);
    }

    public Task<string> CreateRectangleAsync(CadPoint2D corner1, CadPoint2D corner2, double cornerRadius = 0, string? layer = null)
    {
        string h = NextHandle();
        ExecutedOperations.Add($"CreateRectangle: ({corner1.X},{corner1.Y}) to ({corner2.X},{corner2.Y}), Fillet={cornerRadius}");
        var minPt = new CadPoint3D(Math.Min(corner1.X, corner2.X), Math.Min(corner1.Y, corner2.Y), 0);
        var maxPt = new CadPoint3D(Math.Max(corner1.X, corner2.X), Math.Max(corner1.Y, corner2.Y), 0);
        Entities[h] = new CadEntityInfo(h, "102", "Polyline", layer ?? "0", "7", new CadBoundingBox(minPt, maxPt));
        return Task.FromResult(h);
    }

    public Task<string> CreateEllipseAsync(CadPoint3D center, CadPoint3D majorAxisVector, double radiusRatio, string? layer = null)
    {
        string h = NextHandle();
        ExecutedOperations.Add($"CreateEllipse: Center=({center.X},{center.Y})");
        return Task.FromResult(h);
    }

    public Task<bool> MoveEntityAsync(string handle, CadPoint3D fromPoint, CadPoint3D toPoint)
    {
        ExecutedOperations.Add($"MoveEntity: {handle} ({toPoint.X - fromPoint.X},{toPoint.Y - fromPoint.Y})");
        return Task.FromResult(true);
    }

    public Task<string> CopyEntityAsync(string handle, CadPoint3D fromPoint, CadPoint3D toPoint)
    {
        string h = NextHandle();
        ExecutedOperations.Add($"CopyEntity: {handle} -> {h}");
        return Task.FromResult(h);
    }

    public Task<bool> RotateEntityAsync(string handle, CadPoint3D basePoint, double rotationAngleDegrees)
    {
        ExecutedOperations.Add($"RotateEntity: {handle} by {rotationAngleDegrees}°");
        return Task.FromResult(true);
    }

    public Task<bool> ScaleEntityAsync(string handle, CadPoint3D basePoint, double scaleFactor)
    {
        ExecutedOperations.Add($"ScaleEntity: {handle} by {scaleFactor}x");
        return Task.FromResult(true);
    }

    public Task<string> MirrorEntityAsync(string handle, CadPoint3D axisPt1, CadPoint3D axisPt2, bool eraseSource = false)
    {
        string h = NextHandle();
        ExecutedOperations.Add($"MirrorEntity: {handle}");
        return Task.FromResult(h);
    }

    public Task<bool> FilletEntityAsync(string handle1, string handle2, double radius)
    {
        ExecutedOperations.Add($"Fillet: {handle1} & {handle2}, R={radius}");
        return Task.FromResult(true);
    }

    public Task<string> OffsetEntityAsync(string handle, double distance, CadPoint3D sidePoint)
    {
        string h = NextHandle();
        ExecutedOperations.Add($"Offset: {handle} by {distance}");
        return Task.FromResult(h);
    }

    public Task<bool> TrimEntityAsync(string cuttingEdgeHandle, string entityToTrimHandle, CadPoint3D pickPoint)
    {
        ExecutedOperations.Add($"Trim: {entityToTrimHandle}");
        return Task.FromResult(true);
    }

    public Task<bool> ExtendEntityAsync(string boundaryHandle, string entityToExtendHandle, CadPoint3D pickPoint)
    {
        ExecutedOperations.Add($"Extend: {entityToExtendHandle}");
        return Task.FromResult(true);
    }

    public Task<bool> EraseEntityAsync(string handle)
    {
        ExecutedOperations.Add($"Erase: {handle}");
        Entities.Remove(handle);
        return Task.FromResult(true);
    }

    public Task<int> EraseEntitiesAsync(List<string> handles)
    {
        foreach (var h in handles)
        {
            ExecutedOperations.Add($"Erase: {h}");
            Entities.Remove(h);
        }
        return Task.FromResult(handles.Count);
    }

    public Task<string> CreateLinearDimensionAsync(CadPoint3D pt1, CadPoint3D pt2, CadPoint3D textLocation, bool isHorizontal, string? layer = null)
    {
        string h = NextHandle();
        ExecutedOperations.Add($"CreateLinearDimension: Horiz={isHorizontal}, ({pt1.X},{pt1.Y}) to ({pt2.X},{pt2.Y})");
        return Task.FromResult(h);
    }

    public Task<string> CreateAlignedDimensionAsync(CadPoint3D pt1, CadPoint3D pt2, CadPoint3D textLocation, string? layer = null)
    {
        string h = NextHandle();
        ExecutedOperations.Add($"CreateAlignedDimension: ({pt1.X},{pt1.Y}) to ({pt2.X},{pt2.Y})");
        return Task.FromResult(h);
    }

    public Task<string> CreateRadiusDimensionAsync(string circleOrArcHandle, CadPoint3D textLocation, string? layer = null)
    {
        string h = NextHandle();
        ExecutedOperations.Add($"CreateRadiusDimension: {circleOrArcHandle}");
        return Task.FromResult(h);
    }

    public Task<string> CreateDiameterDimensionAsync(string circleOrArcHandle, CadPoint3D textLocation, string? layer = null)
    {
        string h = NextHandle();
        ExecutedOperations.Add($"CreateDiameterDimension: {circleOrArcHandle}");
        return Task.FromResult(h);
    }

    public Task<string> CreateAngularDimensionAsync(CadPoint3D centerPoint, CadPoint3D pt1, CadPoint3D pt2, CadPoint3D arcPoint, string? layer = null)
    {
        string h = NextHandle();
        ExecutedOperations.Add($"CreateAngularDimension");
        return Task.FromResult(h);
    }

    public Task<string> CreateTextAsync(string text, CadPoint3D insertionPoint, double height, double rotationDegrees = 0, string? layer = null)
    {
        string h = NextHandle();
        ExecutedOperations.Add($"CreateText: '{text}' at ({insertionPoint.X},{insertionPoint.Y})");
        return Task.FromResult(h);
    }

    public Task<string> CreateMTextAsync(string text, CadPoint3D insertionPoint, double width, double height, string? layer = null)
    {
        string h = NextHandle();
        ExecutedOperations.Add($"CreateMText: '{text}'");
        return Task.FromResult(h);
    }

    public Task<bool> ZoomExtentsAsync()
    {
        ExecutedOperations.Add("ZoomExtents");
        return Task.FromResult(true);
    }

    public Task<bool> ZoomEntityAsync(string handle)
    {
        ExecutedOperations.Add($"ZoomEntity: {handle}");
        return Task.FromResult(true);
    }

    public Task<bool> ZoomWindowAsync(CadPoint3D pt1, CadPoint3D pt2)
    {
        ExecutedOperations.Add($"ZoomWindow");
        return Task.FromResult(true);
    }
}
