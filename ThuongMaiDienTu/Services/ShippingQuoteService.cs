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

        return await context.ShippingServices
            .AsNoTracking()
            .Where(service =>
                service.Status == "ACTIVE" &&
                service.Provider.Status == "ACTIVE")
            .Select(service => new ShippingSelection(
                service.ProviderId,
                service.ServiceId,
                service.RateRules
                    .Where(rule => rule.Status == "ACTIVE")
                    .Select(rule => (decimal?)rule.Fee)
                    .Min() ?? service.BaseFee,
                service.EstimatedMinDays,
                service.EstimatedMaxDays))
            .OrderBy(quote => quote.Fee)
            .ThenBy(quote => quote.EstimatedMaxDays)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
