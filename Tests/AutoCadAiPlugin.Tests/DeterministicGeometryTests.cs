using AutoCadAiPlugin.Core.Models;
using AutoCadAiPlugin.Tools.GeometryCalculators;
using Xunit;

namespace AutoCadAiPlugin.Tests;

public class DeterministicGeometryTests
{
    [Fact]
    public void CalculateRectangleCenter_ReturnsExactMidpoint()
    {
        var c1 = new CadPoint2D(0, 0);
        var c2 = new CadPoint2D(200, 100);

        var center = DeterministicGeometryEngine.CalculateRectangleCenter(c1, c2);

        Assert.Equal(100.0, center.X);
        Assert.Equal(50.0, center.Y);
    }

    [Fact]
    public void CalculateRectangleFromOriginSize_ReturnsCorrectCorners()
    {
        var origin = new CadPoint2D(50, 50);
        var (c1, c2) = DeterministicGeometryEngine.CalculateRectangleFromOriginSize(origin, 300, 150);

        Assert.Equal(50.0, c1.X);
        Assert.Equal(50.0, c1.Y);
        Assert.Equal(350.0, c2.X);
        Assert.Equal(200.0, c2.Y);
    }

    [Fact]
    public void CalculateRelativePoint3D_OffsetsProperly()
    {
        var basePt = new CadPoint3D(100, 100, 0);
        var offset = DeterministicGeometryEngine.CalculateRelativePoint3D(basePt, 50, -20, 0);

        Assert.Equal(150.0, offset.X);
        Assert.Equal(80.0, offset.Y);
        Assert.Equal(0.0, offset.Z);
    }

    [Fact]
    public void GenerateRegularPolygonVertices_GeneratesRequestedSides()
    {
        var center = new CadPoint2D(0, 0);
        var vertices = DeterministicGeometryEngine.GenerateRegularPolygonVertices(center, 50, 6);

        Assert.Equal(6, vertices.Count);
    }
}
