using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuongMaiDienTu.Data;
using ThuongMaiDienTu.Services;
using ThuongMaiDienTu.ViewModels;

namespace ThuongMaiDienTu.Controllers;

[Authorize(Roles = "ADMIN")]
public sealed class AdminStoresController(
    ThuongMaiDienTuDbContext context,
    IStoreWorkflowService workflow) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status, int page = 1, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        var query = context.Stores.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(item => item.StoreName.Contains(search.Trim()) || item.OwnerUser.Email.Contains(search.Trim()));
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(item => item.Status == status.Trim().ToUpperInvariant());
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(item => item.CreatedAt).Skip((page - 1) * 20).Take(20).Select(item => new AdminStoreListItemViewModel
        {
            StoreId = item.StoreId, StoreName = item.StoreName, SellerName = item.OwnerUser.FullName,
            SellerEmail = item.OwnerUser.Email, Status = item.Status, CreatedAt = item.CreatedAt,
            ProductCount = item.Products.Count()
        }).ToListAsync(cancellationToken);
        return View(new AdminStoreIndexViewModel { Search = search, Status = status, Items = items, Page = page, HasNextPage = total > page * 20 });
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
    {
        var store = await context.Stores.AsNoTracking().Where(item => item.StoreId == id).Select(item => new AdminStoreDetailsViewModel
        {
            Store = new AdminStoreListItemViewModel
            {
                StoreId = item.StoreId, StoreName = item.StoreName, SellerName = item.OwnerUser.FullName,
                SellerEmail = item.OwnerUser.Email, Status = item.Status, CreatedAt = item.CreatedAt,
                ProductCount = item.Products.Count()
            },
            Description = item.Description,
            Slug = item.Slug
        }).SingleOrDefaultAsync(cancellationToken);
        if (store is null) return NotFound();
        store.AuditEntries = await context.AuditLogs.AsNoTracking()
            .Where(log => log.EntityName == "Store" && log.EntityId == id.ToString())
            .OrderByDescending(log => log.CreatedAt)
            .Select(log => log.Action + " - " + (log.AfterJson ?? string.Empty))
            .Take(20).ToListAsync(cancellationToken);
        return View(store);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(StoreStatusInputModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || !await workflow.ChangeStatusAsync(model.StoreId, model.NextStatus, model.Reason!, cancellationToken))
        {
            TempData["ErrorMessage"] = "Trạng thái hoặc lý do không hợp lệ.";
        }
        else TempData["SuccessMessage"] = "Đã cập nhật trạng thái gian hàng.";
        return RedirectToAction(nameof(Index));
    }
}
