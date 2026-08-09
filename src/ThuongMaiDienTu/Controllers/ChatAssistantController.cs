using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ThuongMaiDienTu.Services;
using ThuongMaiDienTu.ViewModels;

namespace ThuongMaiDienTu.Controllers;

[AllowAnonymous]
[AutoValidateAntiforgeryToken]
public sealed class ChatAssistantController(IChatAssistantService assistant) : Controller
{
    [HttpPost("/tro-ly/hoi")]
    [EnableRateLimiting("ChatAssistant")]
    public async Task<IActionResult> Ask(ChatAssistantRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(new { message = "Lựa chọn không hợp lệ." });
        try { return Ok(await assistant.ReplyAsync(request, User, cancellationToken)); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return StatusCode(500, new { message = "Trợ lý tạm thời không khả dụng." }); }
    }
}
