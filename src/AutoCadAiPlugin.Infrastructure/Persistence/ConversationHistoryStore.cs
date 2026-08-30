using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AutoCadAiPlugin.Core.AiContracts;

namespace AutoCadAiPlugin.Infrastructure.Persistence;

public class ConversationHistoryStore
{
    private readonly string _historyDirectory;
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public ConversationHistoryStore(string? customHistoryDir = null)
    {
        if (!string.IsNullOrWhiteSpace(customHistoryDir))
        {
            _historyDirectory = customHistoryDir;
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _historyDirectory = Path.Combine(appData, "Ark4Studio", "Autocat", "history");
        }

        try
        {
            Directory.CreateDirectory(_historyDirectory);
        }
        catch
        {
            // Ignored
        }
    }

    public async Task SaveConversationAsync(AiConversation conversation)
    {
        conversation.UpdatedAt = DateTime.UtcNow;
        string fileName = $"{conversation.Id}.json";
        string filePath = Path.Combine(_historyDirectory, fileName);

        string json = JsonSerializer.Serialize(conversation, _jsonOptions);
        await Task.Run(() => File.WriteAllText(filePath, json));
    }

    public async Task<AiConversation?> LoadConversationAsync(string conversationId)
    {
        string filePath = Path.Combine(_historyDirectory, $"{conversationId}.json");
        if (!File.Exists(filePath)) return null;

        string json = await Task.Run(() => File.ReadAllText(filePath));
        return JsonSerializer.Deserialize<AiConversation>(json);
    }

    public async Task<List<AiConversation>> GetAllConversationsAsync()
    {
        var list = new List<AiConversation>();
        if (!Directory.Exists(_historyDirectory)) return list;

        var files = Directory.GetFiles(_historyDirectory, "*.json");
        foreach (var file in files)
        {
            try
            {
                string json = await Task.Run(() => File.ReadAllText(file));
                var conv = JsonSerializer.Deserialize<AiConversation>(json);
                if (conv != null) list.Add(conv);
            }
            catch
            {
                // Continue with remaining files
            }
        }

        list.Sort((a, b) => b.UpdatedAt.CompareTo(a.UpdatedAt));
        return list;
    }

    public Task DeleteConversationAsync(string conversationId)
    {
        string filePath = Path.Combine(_historyDirectory, $"{conversationId}.json");
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }

    public string ExportConversationToMarkdown(AiConversation conversation)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Chat Session: {conversation.Title}");
        sb.AppendLine($"*Created: {conversation.CreatedAt:yyyy-MM-dd HH:mm:ss UTC}*");
        sb.AppendLine();

        foreach (var msg in conversation.Messages)
        {
            sb.AppendLine($"### **{msg.Role.ToUpperInvariant()}** ({msg.Timestamp:HH:mm:ss})");
            if (!string.IsNullOrWhiteSpace(msg.Content))
            {
                sb.AppendLine(msg.Content);
            }

            if (msg.ToolCalls != null && msg.ToolCalls.Count > 0)
            {
                foreach (var tc in msg.ToolCalls)
                {
                    sb.AppendLine($"> 🔧 **Tool Call**: `{tc.ToolName}`");
                }
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
