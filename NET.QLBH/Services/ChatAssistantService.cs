using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using QLBH.Settings;

namespace QLBH.Services;

public class ChatAssistantService : IChatAssistantService
{
    private readonly HttpClient _httpClient;
    private readonly ChatAssistantSettings _settings;

    public ChatAssistantService(HttpClient httpClient, IOptions<ChatAssistantSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;
    }

    public async Task<string> AskAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        var systemPrompt = string.IsNullOrWhiteSpace(_settings.SystemPrompt)
            ? "Bạn là trợ lý tư vấn bán hàng cho cửa hàng Đồ Gỗ Quảng Thủy. Luôn trả lời bằng tiếng Việt."
            : _settings.SystemPrompt;

        var endpoint = string.IsNullOrWhiteSpace(_settings.Endpoint)
            ? "http://localhost:11434/api/generate"
            : _settings.Endpoint.Trim();

        var payload = new
        {
            model = string.IsNullOrWhiteSpace(_settings.Model) ? "llama3.2" : _settings.Model,
            prompt = $"{systemPrompt}\n\nKhách hỏi: {userMessage}",
            stream = false
        };

        var json = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return $"Trợ lý lỗi {(int)response.StatusCode}: {responseText}";
        }

        using var doc = JsonDocument.Parse(responseText);

        if (doc.RootElement.TryGetProperty("response", out var answer))
        {
            return answer.GetString() ?? "Trợ lý chưa có phản hồi.";
        }

        return "Trợ lý chưa có phản hồi.";
    }
}
