using Microsoft.AspNetCore.Mvc;
using QLBH.Services;

namespace QLBH.Controllers;

[ApiController]
[Route("[controller]")]
public class AIChatController : ControllerBase
{
    private readonly IAIChatService _aiChatService;

    public AIChatController(IAIChatService aiChatService)
    {
        _aiChatService = aiChatService;
    }

    public record ChatRequest(string Message);

    [HttpPost("ask")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Ask([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        var message = request.Message?.Trim();

        if (string.IsNullOrWhiteSpace(message))
        {
            return BadRequest(new { reply = "Bạn hãy nhập câu hỏi trước khi gửi." });
        }

        if (message.Length > 1500)
        {
            return BadRequest(new { reply = "Câu hỏi hơi dài. Bạn rút gọn giúp mình nhé." });
        }

        try
        {
            var reply = await _aiChatService.AskAsync(message, cancellationToken);
            return Ok(new { reply });
        }
        catch
        {
            return Ok(new { reply = "AI đang bận hoặc chưa sẵn sàng. Bạn thử lại sau ít phút nhé." });
        }
    }
}
