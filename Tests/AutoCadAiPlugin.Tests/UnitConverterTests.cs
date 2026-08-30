using AutoCadAiPlugin.Core.Enums;
using AutoCadAiPlugin.Infrastructure.Units;
using Xunit;

namespace AutoCadAiPlugin.Tests;

public class UnitConverterTests
{
    private readonly UnitConverter _converter = new();

    [Theory]
    [InlineData("mm", CadUnitType.Millimeters)]
    [InlineData("میلیمتر", CadUnitType.Millimeters)]
    [InlineData("cm", CadUnitType.Centimeters)]
    [InlineData("inch", CadUnitType.Inches)]
    [InlineData("اینچ", CadUnitType.Inches)]
    public void ParseUnit_CorrectlyMapsUnitStrings(string unitStr, CadUnitType expected)
    {
        var result = _converter.ParseUnit(unitStr);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConvertToCadUnits_ConvertsInchesToMillimetersCorrectly()
    {
        // 2 inches in a millimeter drawing -> 50.8 mm
        double val = _converter.ConvertToCadUnits(2.0, "inch", CadUnitType.Millimeters);
        Assert.Equal(50.8, val, 2);
    }

    [Fact]
    public void ConvertToCadUnits_ConvertsCentimetersToMillimetersCorrectly()
    {
        // 10 cm in a millimeter drawing -> 100 mm
        double val = _converter.ConvertToCadUnits(10.0, "cm", CadUnitType.Millimeters);
        Assert.Equal(100.0, val, 2);
    }
}
