using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuongMaiDienTu.Data;
using ThuongMaiDienTu.Models;
using ThuongMaiDienTu.Services;
using ThuongMaiDienTu.ViewModels;

namespace ThuongMaiDienTu.Controllers;

[Authorize(Roles = "ADMIN,CARRIER")]
public sealed class CarrierPortalController(
    ThuongMaiDienTuDbContext context,
    ICarrierOwnershipService ownership,
    ICurrentUserService currentUser) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var providersQuery = ownership.ScopeProviders(context.ShippingProviders.AsNoTracking());
        var shipmentsQuery = ownership.ScopeShipments(context.Shipments.AsNoTracking());
        var total = await shipmentsQuery.CountAsync();
        var delivered = await shipmentsQuery.CountAsync(item => item.Status == ShipmentWorkflow.Delivered);
        var counts = await shipmentsQuery.GroupBy(item => item.Status)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Key, item => item.Count);
        var providerIds = providersQuery.Select(item => item.ProviderId);

        return View(new CarrierDashboardViewModel
        {
            TotalShipments = total,
            TotalShippingFee = await shipmentsQuery.SumAsync(item => (decimal?)item.ShippingFee) ?? 0,
            SuccessRate = total == 0 ? 0 : decimal.Round(delivered * 100m / total, 1),
            Counts = counts,
            Providers = await providersQuery.OrderBy(item => item.ProviderName)
                .Select(item => new CarrierProviderViewModel
                {
                    ProviderId = item.ProviderId,
                    ProviderName = item.ProviderName,
                    Slug = item.Slug,
                    Status = item.Status,
                    Phone = item.Phone,
                    Email = item.Email,
                    Description = item.Description
                }).ToListAsync(),
            Services = await context.ShippingServices.AsNoTracking()
                .Where(item => providerIds.Contains(item.ProviderId))
                .OrderBy(item => item.Provider.ProviderName).ThenBy(item => item.ServiceName)
                .Select(item => new CarrierServiceViewModel
                {
                    ServiceId = item.ServiceId,
                    ProviderId = item.ProviderId,
                    ProviderName = item.Provider.ProviderName,
                    ServiceCode = item.ServiceCode,
                    ServiceName = item.ServiceName,
                    BaseFee = item.BaseFee,
                    Status = item.Status,
                    Rules = item.RateRules.OrderBy(rule => rule.MinWeight)
                        .Select(rule => new CarrierRateRuleViewModel
                        {
                            RuleId = rule.RuleId,
                            OriginArea = rule.OriginArea,
                            DestinationArea = rule.DestinationArea,
                            MinWeight = rule.MinWeight,
                            MaxWeight = rule.MaxWeight,
                            Fee = rule.Fee,
                            Status = rule.Status
                        }).ToList()
                }).ToListAsync()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProvider(ShippingProviderInputViewModel input)
    {
        var provider = await ownership.ScopeProviders(context.ShippingProviders)
            .SingleOrDefaultAsync(item => item.ProviderId == input.ProviderId);
        if (provider is null) return NotFound();
        input.ProviderName = input.ProviderName?.Trim() ?? string.Empty;
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Thông tin provider không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }
        provider.ProviderName = input.ProviderName;
        provider.Phone = string.IsNullOrWhiteSpace(input.Phone) ? null : input.Phone.Trim();
        provider.Email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim();
        provider.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        provider.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetProviderStatus(long id, string status)
    {
        status = status?.Trim().ToUpperInvariant() ?? string.Empty;
        if (status is not ("ACTIVE" or "PENDING" or "SUSPENDED")) return BadRequest();
        var provider = await context.ShippingProviders.SingleOrDefaultAsync(item => item.ProviderId == id);
        if (provider is null) return NotFound();
        var oldStatus = provider.Status;
        provider.Status = status;
        provider.UpdatedAt = DateTime.UtcNow;
        context.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUser.UserId,
            Action = "SHIPPING_PROVIDER_STATUS_CHANGED",
            EntityName = "ShippingProvider",
            EntityId = id.ToString(),
            BeforeJson = System.Text.Json.JsonSerializer.Serialize(new { Status = oldStatus }),
            AfterJson = System.Text.Json.JsonSerializer.Serialize(new { Status = status }),
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateService(ShippingServiceInputViewModel input)
    {
        Normalize(input);
        var provider = await ownership.ScopeProviders(context.ShippingProviders)
            .SingleOrDefaultAsync(item => item.ProviderId == input.ProviderId);
        if (provider is null) return NotFound();
        if (input.EstimatedMaxDays < input.EstimatedMinDays)
            ModelState.AddModelError(nameof(input.EstimatedMaxDays), "Số ngày tối đa phải lớn hơn hoặc bằng tối thiểu.");
        if (await context.ShippingServices.AnyAsync(item =>
                item.ProviderId == input.ProviderId && item.ServiceCode == input.ServiceCode))
            ModelState.AddModelError(nameof(input.ServiceCode), "Mã dịch vụ đã tồn tại trong provider.");
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Index));
        }
        context.ShippingServices.Add(new ShippingService
        {
            ProviderId = input.ProviderId, ServiceCode = input.ServiceCode,
            ServiceName = input.ServiceName, BaseFee = input.BaseFee,
            EstimatedMinDays = input.EstimatedMinDays,
            EstimatedMaxDays = input.EstimatedMaxDays,
            MaxWeight = input.MaxWeight, Status = "ACTIVE"
        });
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleService(long id)
    {
        var service = await context.ShippingServices
            .Include(item => item.Provider)
            .SingleOrDefaultAsync(item => item.ServiceId == id);
        if (service is null ||
            !await ownership.ScopeProviders(context.ShippingProviders)
                .AnyAsync(item => item.ProviderId == service.ProviderId)) return NotFound();
        service.Status = service.Status == "ACTIVE" ? "INACTIVE" : "ACTIVE";
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRate(ShippingRateInputViewModel input)
    {
        Normalize(input);
        var service = await context.ShippingServices.Include(item => item.Provider)
            .SingleOrDefaultAsync(item => item.ServiceId == input.ServiceId);
        if (service is null ||
            !await ownership.ScopeProviders(context.ShippingProviders)
                .AnyAsync(item => item.ProviderId == service.ProviderId)) return NotFound();
        if (input.MaxWeight <= input.MinWeight)
            ModelState.AddModelError(nameof(input.MaxWeight), "MaxWeight phải lớn hơn MinWeight.");
        var overlaps = await context.ShippingRateRules.AnyAsync(rule =>
            rule.ServiceId == input.ServiceId &&
            rule.OriginArea == input.OriginArea &&
            rule.DestinationArea == input.DestinationArea &&
            input.MinWeight < rule.MaxWeight && input.MaxWeight > rule.MinWeight);
        if (overlaps) ModelState.AddModelError(string.Empty, "Khoảng trọng lượng bị trùng.");
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Index));
        }
        context.ShippingRateRules.Add(new ShippingRateRule
        {
            ServiceId = input.ServiceId, OriginArea = input.OriginArea,
            DestinationArea = input.DestinationArea, MinWeight = input.MinWeight,
            MaxWeight = input.MaxWeight, Fee = input.Fee,
            ExtraFeePerKg = input.ExtraFeePerKg, SupportsCod = input.SupportsCod,
            Status = "ACTIVE"
        });
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleRate(long id)
    {
        var rule = await context.ShippingRateRules.Include(item => item.Service)
            .ThenInclude(item => item.Provider).SingleOrDefaultAsync(item => item.RuleId == id);
        if (rule is null ||
            !await ownership.ScopeProviders(context.ShippingProviders)
                .AnyAsync(item => item.ProviderId == rule.Service.ProviderId)) return NotFound();
        rule.Status = rule.Status == "ACTIVE" ? "INACTIVE" : "ACTIVE";
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private static void Normalize(ShippingServiceInputViewModel input)
    {
        input.ServiceCode = input.ServiceCode.Trim().ToUpperInvariant();
        input.ServiceName = input.ServiceName.Trim();
    }
    private static void Normalize(ShippingRateInputViewModel input)
    {
        input.OriginArea = input.OriginArea.Trim().ToUpperInvariant();
        input.DestinationArea = input.DestinationArea.Trim().ToUpperInvariant();
    }
}
