using Microsoft.EntityFrameworkCore;
using ThuongMaiDienTu.Data;

namespace ThuongMaiDienTu.Services;

public sealed record ShippingSelection(
    long ProviderId,
    long ServiceId,
    decimal Fee,
    int EstimatedMinDays,
    int EstimatedMaxDays);

public interface IShippingQuoteService
{
    Task<ShippingSelection?> GetBestInternalQuoteAsync(
        long storeId,
        string destination,
        CancellationToken cancellationToken = default);
}

public sealed class ShippingQuoteService(ThuongMaiDienTuDbContext context)
    : IShippingQuoteService
{
    public async Task<ShippingSelection?> GetBestInternalQuoteAsync(
        long storeId,
        string destination,
        CancellationToken cancellationToken = default)
    {
        if (storeId <= 0 || string.IsNullOrWhiteSpace(destination))
        {
            return null;
        }

        return await context.StoreShippingServices
            .AsNoTracking()
            .Where(mapping =>
                mapping.StoreId == storeId &&
                mapping.IsEnabled &&
                mapping.Status == "ACTIVE" &&
                mapping.Service.Status == "ACTIVE" &&
                mapping.Service.Provider.Status == "ACTIVE")
            .Select(mapping => new
            {
                mapping.Service.ProviderId,
                mapping.ServiceId,
                Fee = mapping.FeeOverride ?? mapping.Service.RateRules
                    .Where(rule => rule.Status == "ACTIVE")
                    .Select(rule => (decimal?)rule.Fee)
                    .Min() ?? mapping.Service.BaseFee,
                mapping.Service.EstimatedMinDays,
                mapping.Service.EstimatedMaxDays
            })
            .OrderBy(candidate => candidate.Fee)
            .ThenBy(candidate => candidate.EstimatedMaxDays)
            .Select(candidate => new ShippingSelection(
                candidate.ProviderId,
                candidate.ServiceId,
                candidate.Fee,
                candidate.EstimatedMinDays,
                candidate.EstimatedMaxDays))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
