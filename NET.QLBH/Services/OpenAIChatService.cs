using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using QLBH.Settings;

namespace QLBH.Services;

public class OpenAIChatService : IAIChatService
{
    private readonly HttpClient _httpClient;
    private readonly AIChatSettings _settings;

    public OpenAIChatService(HttpClient httpClient, IOptions<AIChatSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<string> AskAsync(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey) || string.IsNullOrWhiteSpace(_settings.Model))
        {
            return "Chat AI hiện chưa được cấu hình API key hoặc model.";
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        var payload = new
        {
            model = _settings.Model,
            messages = new object[]
            {
                new { role = "system", content = _settings.SystemPrompt },
                new { role = "user", content = message }
            },
            temperature = 0.4,
            max_completion_tokens = 400
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return "AI tạm thời chưa phản hồi được. Bạn hãy thử lại sau ít phút.";
        }

        using var doc = JsonDocument.Parse(responseText);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return string.IsNullOrWhiteSpace(content)
            ? "Mình chưa có câu trả lời phù hợp. Bạn thử mô tả chi tiết hơn nhé."
            : content.Trim();
    }
}
