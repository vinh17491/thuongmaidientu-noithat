using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThuongMaiDienTu.Services;
using ThuongMaiDienTu.ViewModels;

namespace ThuongMaiDienTu.Controllers;

[AllowAnonymous]
[AutoValidateAntiforgeryToken]
public sealed class ChatAssistantController(IChatAssistantService assistant) : Controller
{
    private static readonly ConcurrentDictionary<string, Queue<DateTime>> Requests = new();
    [HttpPost("/tro-ly/hoi")]
    public async Task<IActionResult> Ask(ChatAssistantRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(new { message = "Vui lòng nhập câu hỏi tối đa 500 ký tự." });
        var key = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"; var now = DateTime.UtcNow; var queue = Requests.GetOrAdd(key, _ => new Queue<DateTime>());
        lock (queue) { while (queue.Count > 0 && now - queue.Peek() > TimeSpan.FromMinutes(1)) queue.Dequeue(); if (queue.Count >= 15) return StatusCode(429, new { message = "Bạn gửi quá nhiều yêu cầu, vui lòng thử lại sau." }); queue.Enqueue(now); }
        try { return Ok(await assistant.ReplyAsync(request, User, cancellationToken)); } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; } catch { return StatusCode(500, new { message = "Trợ lý tạm thời không khả dụng." }); }
    }
}
