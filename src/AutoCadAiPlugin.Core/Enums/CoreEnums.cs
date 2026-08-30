namespace AutoCadAiPlugin.Core.Enums;

public enum AiProviderType
{
    OpenAI,
    Gemini,
    Anthropic,
    Mock
}

public enum ToolExecutionStatus
{
    Pending,
    RequiresConfirmation,
    Executing,
    Completed,
    Failed,
    Cancelled
}

public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum CadUnitType
{
    Unitless = 0,
    Inches = 1,
    Feet = 2,
    Miles = 3,
    Millimeters = 4,
    Centimeters = 5,
    Meters = 6,
    Kilometers = 7,
    Microinches = 8,
    Mils = 9,
    Yards = 10,
    Angstroms = 11,
    Nanometers = 12,
    Microns = 13,
    Decimeters = 14,
    Dekameters = 15,
    Hectometers = 16,
    Gigameters = 17,
    Astronomical = 18,
    LightYears = 19,
    Parsecs = 20
}
