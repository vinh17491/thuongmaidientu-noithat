using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuongMaiDienTu.Data;
using ThuongMaiDienTu.Models;
using ThuongMaiDienTu.Services;
using ThuongMaiDienTu.ViewModels;

namespace ThuongMaiDienTu.Controllers;

[Authorize(Roles = "SELLER")]
public sealed class SellerStoresController(
    ThuongMaiDienTuDbContext context,
    ICurrentUserService currentUser,
    IStoreOwnershipService ownership) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var store = currentUser.UserId.HasValue
            ? await context.Stores.AsNoTracking().FirstOrDefaultAsync(item => item.OwnerUserId == currentUser.UserId.Value, cancellationToken)
            : null;
        return View(store is null ? null : Map(store));
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        if (await ownership.HasOwnedStoreAsync(cancellationToken)) return RedirectToAction(nameof(Index));
        return View(new StoreProfileInputModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StoreProfileInputModel model, CancellationToken cancellationToken)
    {
        if (await ownership.HasOwnedStoreAsync(cancellationToken))
        {
            ModelState.AddModelError(string.Empty, "Tài khoản đã có gian hàng.");
        }
        if (!ModelState.IsValid) return View(model);
        var store = new Store
        {
            OwnerUserId = currentUser.UserId!.Value,
            StoreName = model.StoreName.Trim(),
            Slug = await StoreSlugService.CreateUniqueAsync(context, model.StoreName, cancellationToken: cancellationToken),
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            SeoTitle = model.SeoTitle,
            SeoDescription = model.SeoDescription,
            Status = StoreStatuses.Pending,
            CreatedAt = DateTime.UtcNow
        };
        context.Stores.Add(store);
        await context.SaveChangesAsync(cancellationToken);
        context.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUser.UserId,
            Action = "STORE_ONBOARDING_SUBMITTED",
            EntityName = "Store",
            EntityId = store.StoreId.ToString(),
            AfterJson = System.Text.Json.JsonSerializer.Serialize(new { store.StoreName, store.Slug, store.Status }),
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync(cancellationToken);
        TempData["SuccessMessage"] = "Đã gửi hồ sơ gian hàng chờ duyệt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(CancellationToken cancellationToken)
    {
        var store = currentUser.UserId.HasValue
            ? await context.Stores.AsNoTracking().FirstOrDefaultAsync(item => item.OwnerUserId == currentUser.UserId.Value, cancellationToken)
            : null;
        if (store is null) return NotFound();
        return View(new StoreProfileInputModel { StoreId = store.StoreId, StoreName = store.StoreName, Description = store.Description, SeoTitle = store.SeoTitle, SeoDescription = store.SeoDescription });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(StoreProfileInputModel model, CancellationToken cancellationToken)
    {
        var store = model.StoreId.HasValue ? await ownership.GetOwnedStoreAsync(model.StoreId.Value, cancellationToken) : null;
        if (store is null) return NotFound();
        if (!ModelState.IsValid) return View(model);
        store.StoreName = model.StoreName.Trim();
        store.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        store.SeoTitle = model.SeoTitle;
        store.SeoDescription = model.SeoDescription;
        await context.SaveChangesAsync(cancellationToken);
        TempData["SuccessMessage"] = "Đã cập nhật hồ sơ gian hàng.";
        return RedirectToAction(nameof(Index));
    }

    private static StoreProfileViewModel Map(Store store) => new()
    {
        StoreId = store.StoreId, StoreName = store.StoreName, Slug = store.Slug,
        Description = store.Description, Status = store.Status, SeoTitle = store.SeoTitle,
        SeoDescription = store.SeoDescription, CreatedAt = store.CreatedAt
    };
}
