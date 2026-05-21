using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;
using AgenticSecuritySimulator.Core.Entities;

namespace AgenticSecuritySimulator.Agents;

public sealed class AiChatClientFactory
{
    private readonly IAiSettingProvider _settingProvider;
    private readonly HttpClient _httpClient;

    public AiChatClientFactory(IAiSettingProvider settingProvider, HttpClient httpClient)
    {
        _settingProvider = settingProvider;
        _httpClient = httpClient;
    }

    public async Task<IChatClient?> GetActiveClientAsync(CancellationToken cancellationToken = default)
    {
        var activeSetting = await _settingProvider.GetActiveSettingAsync(cancellationToken);
        if (activeSetting is null)
            return null;

        return new HttpClientChatClient(_httpClient, activeSetting.ProviderType, activeSetting.Endpoint, activeSetting.ModelName, activeSetting.ApiKey);
    }
}

public sealed class HttpClientChatClient : IChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string _providerType;
    private readonly string _endpoint;
    private readonly string _modelName;
    private readonly string? _apiKey;

    public HttpClientChatClient(HttpClient httpClient, string providerType, string endpoint, string modelName, string? apiKey)
    {
        _httpClient = httpClient;
        _providerType = providerType;
        _endpoint = endpoint.TrimEnd('/');
        _modelName = modelName;
        _apiKey = apiKey;
    }

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        string requestUrl;
        if (_providerType.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase))
        {
            requestUrl = $"{_endpoint}/openai/deployments/{_modelName}/chat/completions?api-version=2024-02-15-preview";
        }
        else
        {
            // Ollama / LM Studio standard completions endpoint
            requestUrl = $"{_endpoint}/v1/chat/completions";
        }

        var messagesJson = chatMessages.Select(m => new
        {
            role = m.Role.ToString().ToLowerInvariant(),
            content = m.Text
        }).ToList();

        var payload = new
        {
            model = _modelName,
            messages = messagesJson,
            temperature = options?.Temperature ?? 0.7f,
            max_tokens = options?.MaxOutputTokens ?? 1020
        };

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        if (!string.IsNullOrEmpty(_apiKey))
        {
            if (_providerType.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase))
            {
                requestMessage.Headers.Add("api-key", _apiKey);
            }
            else
            {
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
            }
        }

        var json = JsonSerializer.Serialize(payload);
        requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        string completionText = "";
        if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
        {
            var choice = choices[0];
            if (choice.TryGetProperty("message", out var msg) && msg.TryGetProperty("content", out var content))
            {
                completionText = content.GetString() ?? "";
            }
        }

        var responseMessage = new ChatMessage(ChatRole.Assistant, completionText);
        return new ChatResponse(responseMessage)
        {
            ResponseId = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : Guid.NewGuid().ToString(),
            ModelId = _modelName
        };
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Streaming response is not supported by the Agentic Security Simulator Monte Carlo engine.");
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    
    public void Dispose() { }
}
