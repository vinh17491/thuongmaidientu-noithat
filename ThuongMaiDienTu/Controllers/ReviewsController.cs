using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuongMaiDienTu.Data;
using ThuongMaiDienTu.Models;
using ThuongMaiDienTu.Services;
using ThuongMaiDienTu.ViewModels;

namespace ThuongMaiDienTu.Controllers;

[Authorize(Roles = "CUSTOMER")]
public class ReviewsController : Controller
{
    private readonly ThuongMaiDienTuDbContext _context;

    public ReviewsController(ThuongMaiDienTuDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Create(long orderItemId)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Forbid();
        }

        var eligibility = await LoadEligibleOrderItemAsync(orderItemId, userId);
        if (eligibility is null)
        {
            return NotFound();
        }

        if (eligibility.Order.OrderStatus != OrderWorkflowHelper.Delivered)
        {
            TempData["ErrorMessage"] = "Chỉ sản phẩm thuộc đơn đã giao mới được đánh giá.";
            return RedirectToAction(
                "Details",
                "Orders",
                new { id = eligibility.OrderId });
        }

        if (await HasReviewAsync(userId, orderItemId))
        {
            TempData["ErrorMessage"] = "Bạn đã đánh giá sản phẩm trong đơn hàng này.";
            return RedirectToAction(
                "Details",
                "Orders",
                new { id = eligibility.OrderId });
        }

        return View(new ReviewCreateViewModel
        {
            OrderItemId = eligibility.OrderItemId,
            OrderId = eligibility.OrderId,
            ProductName = eligibility.ProductName,
            Rating = 5
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReviewCreateViewModel model)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Forbid();
        }

        model.Comment = string.IsNullOrWhiteSpace(model.Comment)
            ? null
            : model.Comment.Trim();

        var orderItem = await LoadEligibleOrderItemAsync(model.OrderItemId, userId);
        if (orderItem is null)
        {
            return NotFound();
        }

        model.OrderId = orderItem.OrderId;
        model.ProductName = orderItem.ProductName;

        if (orderItem.Order.OrderStatus != OrderWorkflowHelper.Delivered)
        {
            ModelState.AddModelError(
                string.Empty,
                "Chỉ sản phẩm thuộc đơn đã giao mới được đánh giá.");
        }

        if (await HasReviewAsync(userId, model.OrderItemId))
        {
            ModelState.AddModelError(
                string.Empty,
                "Bạn đã đánh giá sản phẩm trong đơn hàng này.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var review = new Review
        {
            UserId = userId,
            ProductId = orderItem.Sku.ProductId,
            OrderItemId = orderItem.OrderItemId,
            Rating = model.Rating,
            Comment = model.Comment,
            Status = "VISIBLE",
            CreatedAt = DateTime.Now
        };

        _context.Reviews.Add(review);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "Không thể lưu đánh giá. Sản phẩm này có thể đã được đánh giá.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Cảm ơn bạn đã đánh giá sản phẩm.";
        return RedirectToAction(
            "Details",
            "Orders",
            new { id = orderItem.OrderId });
    }

    private async Task<OrderItem?> LoadEligibleOrderItemAsync(long orderItemId, long userId)
    {
        return await _context.OrderItems
            .AsNoTracking()
            .Include(item => item.Order)
            .Include(item => item.Sku)
                .ThenInclude(item => item.Product)
            .SingleOrDefaultAsync(item =>
                item.OrderItemId == orderItemId &&
                item.Order.UserId == userId);
    }

    private Task<bool> HasReviewAsync(long userId, long orderItemId)
    {
        return _context.Reviews
            .AsNoTracking()
            .AnyAsync(item =>
                item.UserId == userId &&
                item.OrderItemId == orderItemId);
    }

    private bool TryGetCurrentUserId(out long userId)
    {
        return long.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
    }
}
