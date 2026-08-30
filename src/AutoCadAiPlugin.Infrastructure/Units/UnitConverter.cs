using System;
using System.Text.RegularExpressions;
using AutoCadAiPlugin.Core.Enums;
using AutoCadAiPlugin.Core.Interfaces;

namespace AutoCadAiPlugin.Infrastructure.Units;

public class UnitConverter : IUnitConverter
{
    private static readonly Regex NumberWithUnitPattern = new(@"^([\d\.\-]+)\s*([a-zA-Z]+)?$", RegexOptions.Compiled);

    public double ConvertToCadUnits(double value, string unitString, CadUnitType activeDrawingUnit)
    {
        if (string.IsNullOrWhiteSpace(unitString))
            return value;

        var sourceUnit = ParseUnit(unitString);
        if (sourceUnit == CadUnitType.Unitless || sourceUnit == activeDrawingUnit)
            return value;

        // Convert source unit to Millimeters base
        double mmValue = sourceUnit switch
        {
            CadUnitType.Millimeters => value,
            CadUnitType.Centimeters => value * 10.0,
            CadUnitType.Meters => value * 1000.0,
            CadUnitType.Inches => value * 25.4,
            CadUnitType.Feet => value * 304.8,
            _ => value
        };

        // Convert Millimeters base to active Drawing unit
        return activeDrawingUnit switch
        {
            CadUnitType.Millimeters => mmValue,
            CadUnitType.Centimeters => mmValue / 10.0,
            CadUnitType.Meters => mmValue / 1000.0,
            CadUnitType.Inches => mmValue / 25.4,
            CadUnitType.Feet => mmValue / 304.8,
            _ => value
        };
    }

    public double ConvertFromCadUnits(double value, string targetUnitString, CadUnitType activeDrawingUnit)
    {
        if (string.IsNullOrWhiteSpace(targetUnitString))
            return value;

        var targetUnit = ParseUnit(targetUnitString);
        if (targetUnit == CadUnitType.Unitless || targetUnit == activeDrawingUnit)
            return value;

        // Convert active drawing unit to mm base
        double mmValue = activeDrawingUnit switch
        {
            CadUnitType.Millimeters => value,
            CadUnitType.Centimeters => value * 10.0,
            CadUnitType.Meters => value * 1000.0,
            CadUnitType.Inches => value * 25.4,
            CadUnitType.Feet => value * 304.8,
            _ => value
        };

        // Convert mm base to target unit
        return targetUnit switch
        {
            CadUnitType.Millimeters => mmValue,
            CadUnitType.Centimeters => mmValue / 10.0,
            CadUnitType.Meters => mmValue / 1000.0,
            CadUnitType.Inches => mmValue / 25.4,
            CadUnitType.Feet => mmValue / 304.8,
            _ => value
        };
    }

    public CadUnitType ParseUnit(string unitString)
    {
        if (string.IsNullOrWhiteSpace(unitString)) return CadUnitType.Unitless;

        string clean = unitString.Trim().ToLowerInvariant();
        return clean switch
        {
            "mm" or "millimeter" or "millimeters" or "میلیمتر" or "میلی‌متر" => CadUnitType.Millimeters,
            "cm" or "centimeter" or "centimeters" or "سانتیمتر" or "سانتی‌متر" => CadUnitType.Centimeters,
            "m" or "meter" or "meters" or "متر" => CadUnitType.Meters,
            "in" or "inch" or "inches" or "اینچ" => CadUnitType.Inches,
            "ft" or "foot" or "feet" or "فوت" or "پا" => CadUnitType.Feet,
            _ => CadUnitType.Unitless
        };
    }
}
