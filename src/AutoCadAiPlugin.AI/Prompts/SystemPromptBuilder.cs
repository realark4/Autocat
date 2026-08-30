using System.Text;
using System.Threading.Tasks;
using AutoCadAiPlugin.Core.AiContracts;
using AutoCadAiPlugin.Core.Interfaces;

namespace AutoCadAiPlugin.AI.Prompts;

public static class SystemPromptBuilder
{
    public static async Task<string> BuildSystemPromptAsync(
        AiProviderConfig config,
        ICadService? cadService)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are Autocat, an elite Senior AutoCAD AI Assistant integrated directly inside Autodesk AutoCAD.");
        sb.AppendLine("Your goal is to understand user drafting intents in both Persian (فارسی) and English and translate them into precise native AutoCAD drawing actions using tool calls.");
        sb.AppendLine();
        sb.AppendLine("CRITICAL OPERATIONAL RULES:");
        sb.AppendLine("1. You CANNOT directly draw or modify anything yourself. You MUST call the appropriate whitelisted CAD tools.");
        sb.AppendLine("2. NEVER assume or hallucinate that an action succeeded. Only declare success after receiving a successful tool execution result.");
        sb.AppendLine("3. For coordinate reasoning: use exact mathematical and geometric coordinates. For example, the center of a rectangle from (0,0) to (200,100) is (100,50).");
        sb.AppendLine("4. Multi-step tasks: If a user asks for compound geometry (e.g. 'draw a 200x100 plate, put a Ø40 hole in the center, and add dimensions'), execute the required tools in logical order.");
        sb.AppendLine("5. Selection intelligence: If the user says 'this circle' or 'the selected object', first call get_selected_entities to inspect what is currently selected.");
        sb.AppendLine("6. Clarification: If a request is genuinely ambiguous (e.g. 'draw a line' without any length, direction, or coordinates), politely ask the user for details.");
        sb.AppendLine("7. Language: If the user asks in Persian, respond in fluent, professional Persian (فارسی). If the user asks in English, respond in English. Technical tool names and parameters remain in standard English.");
        sb.AppendLine();

        if (config.SendDrawingContext && cadService != null)
        {
            try
            {
                var dwgInfo = await cadService.GetDrawingInfoAsync();
                var selected = await cadService.GetSelectedEntitiesAsync();

                sb.AppendLine("ACTIVE DRAWING CONTEXT:");
                sb.AppendLine($"- Document Name: {dwgInfo.DocumentName}");
                sb.AppendLine($"- Active Space: {dwgInfo.ActiveSpace}");
                sb.AppendLine($"- Active Layer: {dwgInfo.ActiveLayer}");
                sb.AppendLine($"- Drawing Linear Units: {dwgInfo.LinearUnits}");
                sb.AppendLine($"- Available Layers: {string.Join(", ", dwgInfo.LayerNames)}");
                sb.AppendLine($"- Selected Entities Count: {selected.Count}");

                if (selected.Count > 0)
                {
                    sb.AppendLine("CURRENTLY SELECTED ENTITIES:");
                    foreach (var ent in selected)
                    {
                        sb.AppendLine($"  * Handle: {ent.Handle}, Type: {ent.EntityType}, Layer: {ent.Layer}");
                        if (ent.BoundingBox != null)
                        {
                            sb.AppendLine($"    Bounds Center: ({ent.BoundingBox.Center.X:F2}, {ent.BoundingBox.Center.Y:F2}), Width: {ent.BoundingBox.Width:F2}, Height: {ent.BoundingBox.Height:F2}");
                        }
                    }
                }
                sb.AppendLine();
            }
            catch
            {
                // Drawing context query failure fallback
            }
        }

        return sb.ToString();
    }
}
