using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuongMaiDienTu.Data;
using ThuongMaiDienTu.ViewModels;

namespace ThuongMaiDienTu.Controllers;

[Authorize(Roles = "ADMIN,SELLER")]
public class ReviewAdminController : Controller
{
    private static readonly string[] ValidStatuses = ["VISIBLE", "HIDDEN"];
    private readonly ThuongMaiDienTuDbContext _context;

    public ReviewAdminController(ThuongMaiDienTuDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? status)
    {
        status = string.IsNullOrWhiteSpace(status)
            ? null
            : status.Trim().ToUpperInvariant();

        if (status is not null && !ValidStatuses.Contains(status))
        {
            status = null;
        }

        var query = _context.Reviews.AsNoTracking();
        if (status is not null)
        {
            query = query.Where(item => item.Status == status);
        }

        var model = await query
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.ReviewId)
            .Select(item => new ReviewAdminListItemViewModel
            {
                ReviewId = item.ReviewId,
                CustomerName = item.User.FullName,
                ProductName = item.Product.ProductName,
                OrderItemId = item.OrderItemId,
                OrderCode = item.OrderItem == null
                    ? null
                    : item.OrderItem.Order.OrderCode,
                Rating = item.Rating,
                Comment = item.Comment,
                Status = item.Status,
                CreatedAt = item.CreatedAt
            })
            .ToListAsync();

        ViewBag.Status = status;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(long id, string? statusFilter)
    {
        var review = await _context.Reviews
            .SingleOrDefaultAsync(item => item.ReviewId == id);

        if (review is null)
        {
            return NotFound();
        }

        review.Status = review.Status == "VISIBLE" ? "HIDDEN" : "VISIBLE";

        try
        {
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] =
                review.Status == "VISIBLE"
                    ? "Đã hiển thị đánh giá."
                    : "Đã ẩn đánh giá.";
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] =
                "Không thể cập nhật trạng thái đánh giá lúc này.";
        }

        return RedirectToAction(nameof(Index), new { status = statusFilter });
    }
}
