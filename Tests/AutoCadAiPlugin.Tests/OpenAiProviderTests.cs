using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoCadAiPlugin.AI.Providers;
using AutoCadAiPlugin.Core.AiContracts;
using AutoCadAiPlugin.Core.Enums;
using Xunit;

namespace AutoCadAiPlugin.Tests;

public class OpenAiProviderTests
{
    [Fact]
    public async Task SendMessage_UsesCustomEndpointAndManualModel()
    {
        var handler = new RecordingHandler(_ => JsonResponse(
            HttpStatusCode.OK,
            "{\"choices\":[{\"message\":{\"content\":\"proxy response\"}}]}"));
        using var httpClient = new HttpClient(handler);
        var provider = new OpenAiProvider(httpClient);
        var config = new AiProviderConfig
        {
            ProviderType = AiProviderType.OpenAI,
            BaseUrl = "https://gateway.example.com/v1/chat/completions/",
            Model = "deepseek/deepseek-chat",
            ApiKey = "proxy-key"
        };

        var response = await provider.SendMessageAsync(
            new List<AiMessage> { AiMessage.User("draw a rectangle") },
            new List<AutoCadAiPlugin.Core.ToolContracts.ToolDefinition>(),
            config);

        Assert.Null(response.ErrorMessage);
        Assert.Equal("proxy response", response.ContentText);
        Assert.Equal("https://gateway.example.com/v1/chat/completions", handler.RequestUris[0].ToString());
        Assert.Equal("Bearer proxy-key", handler.AuthorizationHeaders[0]);

        using var payload = JsonDocument.Parse(handler.RequestBodies[0]);
        Assert.Equal("deepseek/deepseek-chat", payload.RootElement.GetProperty("model").GetString());
    }

    [Fact]
    public async Task ValidateConnection_FallsBackToChatWhenModelsEndpointIsUnavailable()
    {
        var handler = new RecordingHandler(request => request.Method == HttpMethod.Get
            ? JsonResponse(HttpStatusCode.NotFound, "not implemented")
            : JsonResponse(HttpStatusCode.OK, "{\"choices\":[{\"message\":{\"content\":\"ok\"}}]}"));
        using var httpClient = new HttpClient(handler);
        var provider = new OpenAiProvider(httpClient);

        bool connected = await provider.ValidateConnectionAsync(
            string.Empty,
            "llama3.2",
            "http://localhost:11434/v1/");

        Assert.True(connected);
        Assert.Equal("http://localhost:11434/v1/models", handler.RequestUris[0].ToString());
        Assert.Equal("http://localhost:11434/v1/chat/completions", handler.RequestUris[1].ToString());
        Assert.Null(handler.AuthorizationHeaders[1]);
    }

    [Fact]
    public async Task GetSupportedModels_DoesNotFilterProxyModelIds()
    {
        var handler = new RecordingHandler(_ => JsonResponse(
            HttpStatusCode.OK,
            "{\"data\":[{\"id\":\"deepseek/deepseek-chat\"},{\"id\":\"mistralai/mistral-small\"}]}"));
        using var httpClient = new HttpClient(handler);
        var provider = new OpenAiProvider(httpClient);

        var models = await provider.GetSupportedModelsAsync(
            "proxy-key",
            "https://gateway.example.com/v1");

        Assert.Contains("deepseek/deepseek-chat", models);
        Assert.Contains("mistralai/mistral-small", models);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public List<Uri> RequestUris { get; } = new();
        public List<string> RequestBodies { get; } = new();
        public List<string?> AuthorizationHeaders { get; } = new();

        public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri!);
            AuthorizationHeaders.Add(request.Headers.Authorization?.ToString());
            if (request.Content != null)
            {
                RequestBodies.Add(await request.Content.ReadAsStringAsync());
            }
            else
            {
                RequestBodies.Add(string.Empty);
            }

            return _responseFactory(request);
        }
    }
}
