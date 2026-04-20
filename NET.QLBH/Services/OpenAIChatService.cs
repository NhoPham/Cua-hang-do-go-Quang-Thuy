using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using QLBH.Settings;

namespace QLBH.Services
{
    public class OpenAIChatService : IAIChatService
    {
        private readonly HttpClient _httpClient;
        private readonly AIChatSettings _settings;

        public OpenAIChatService(HttpClient httpClient, IOptions<AIChatSettings> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;
        }

        public async Task<string> AskAsync(string userMessage, CancellationToken cancellationToken = default)
        {
            var systemPrompt = string.IsNullOrWhiteSpace(_settings.SystemPrompt)
                ? "Bạn là trợ lý tư vấn bán hàng cho cửa hàng Đồ Gỗ Quảng Thủy. Luôn trả lời bằng tiếng Việt."
                : _settings.SystemPrompt;

            var payload = new
            {
                model = string.IsNullOrWhiteSpace(_settings.Model) ? "llama3.2" : _settings.Model,
                prompt = $"{systemPrompt}\n\nKhách hỏi: {userMessage}",
                stream = false
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync(
                "http://localhost:11434/api/generate",
                content,
                cancellationToken
            );

            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return $"Ollama lỗi {(int)response.StatusCode}: {responseText}";
            }

            using var doc = JsonDocument.Parse(responseText);

            if (doc.RootElement.TryGetProperty("response", out var answer))
            {
                return answer.GetString() ?? "AI chưa có phản hồi.";
            }

            return "AI chưa có phản hồi.";
        }
    }
}