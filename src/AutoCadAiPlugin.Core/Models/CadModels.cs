using System;
using System.Collections.Generic;
using AutoCadAiPlugin.Core.Enums;

namespace AutoCadAiPlugin.Core.Models;

public record CadPoint2D(double X, double Y)
{
    public static CadPoint2D Origin => new(0, 0);

    public double DistanceTo(CadPoint2D other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public CadPoint2D Offset(double dx, double dy) => new(X + dx, Y + dy);

    public CadPoint2D MidPointWith(CadPoint2D other) => new((X + other.X) / 2.0, (Y + other.Y) / 2.0);

    public CadPoint3D To3D(double z = 0.0) => new(X, Y, z);
}

public record CadPoint3D(double X, double Y, double Z = 0.0)
{
    public static CadPoint3D Origin => new(0, 0, 0);

    public double DistanceTo(CadPoint3D other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        double dz = Z - other.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public CadPoint3D Offset(double dx, double dy, double dz = 0.0) => new(X + dx, Y + dy, Z + dz);

    public CadPoint3D MidPointWith(CadPoint3D other) => new((X + other.X) / 2.0, (Y + other.Y) / 2.0, (Z + other.Z) / 2.0);

    public CadPoint2D To2D() => new(X, Y);
}

public record CadBoundingBox(CadPoint3D MinPoint, CadPoint3D MaxPoint)
{
    public double Width => Math.Abs(MaxPoint.X - MinPoint.X);
    public double Height => Math.Abs(MaxPoint.Y - MinPoint.Y);
    public double Depth => Math.Abs(MaxPoint.Z - MinPoint.Z);

    public CadPoint3D Center => new(
        (MinPoint.X + MaxPoint.X) / 2.0,
        (MinPoint.Y + MaxPoint.Y) / 2.0,
        (MinPoint.Z + MaxPoint.Z) / 2.0
    );
}

public record CadEntityInfo(
    string Handle,
    string ObjectIdString,
    string EntityType,
    string Layer,
    string Color,
    CadBoundingBox? BoundingBox,
    Dictionary<string, object>? GeometricProperties = null
);

public record CadDrawingInfo(
    string DocumentName,
    string DatabasePath,
    string ActiveSpace,
    string ActiveLayer,
    string CurrentUcs,
    CadUnitType LinearUnits,
    int EntityCount,
    List<string> LayerNames
);

public record CadLayerInfo(
    string Name,
    string Color,
    bool IsLocked,
    bool IsFrozen,
    bool IsOff
);
