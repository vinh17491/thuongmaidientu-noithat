using Microsoft.EntityFrameworkCore;
using ThuongMaiDienTu.Data;
using ThuongMaiDienTu.Services;
using ThuongMaiDienTu.Controllers;
using ThuongMaiDienTu.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ThuongMaiDienTu.Tests;

[Collection(SqlServerConfigurationCollection.Name)]
public sealed class SqlServerMarketplaceIntegrationTests
{
    [Fact]
    public async Task CanonicalShippingSchema_IsQueryableByEf()
    {
        await using var context = CreateContext();

        Assert.True(await context.ShippingProviders.AnyAsync());
        Assert.True(await context.ShippingServices.AnyAsync());
        Assert.True(await context.StoreShippingServices.AnyAsync());
        _ = await context.ShippingRateRules.CountAsync();
        _ = await context.ShippingQuotes.CountAsync();
        _ = await context.Shipments.CountAsync();
        _ = await context.ShipmentStatusHistories.CountAsync();
        _ = await context.OrderStatusHistories.CountAsync();
        _ = await context.AuditLogs.CountAsync();
    }

    [Fact]
    public async Task QuoteSelection_ReturnsOnlyAServiceEnabledForTheStore()
    {
        await using var context = CreateContext();
        var mapping = await context.StoreShippingServices
            .AsNoTracking()
            .Where(item =>
                item.IsEnabled &&
                item.Status == "ACTIVE" &&
                item.Service.Status == "ACTIVE" &&
                item.Service.Provider.Status == "ACTIVE")
            .OrderBy(item => item.StoreId)
            .FirstAsync();

        var quote = await new ShippingQuoteService(context)
            .GetBestInternalQuoteAsync(mapping.StoreId, "Ho Chi Minh");

        Assert.NotNull(quote);
        Assert.True(await context.StoreShippingServices.AnyAsync(item =>
            item.StoreId == mapping.StoreId &&
            item.ServiceId == quote.ServiceId &&
            item.IsEnabled &&
            item.Status == "ACTIVE"));
    }

    [Fact]
    public async Task DatabaseConstraints_AreTrustedAndIndexesEnabled()
    {
        await using var context = CreateContext();

        var badForeignKeys = await context.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1")
            .SingleAsync();
        var badChecks = await context.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM sys.check_constraints WHERE is_disabled=1 OR is_not_trusted=1")
            .SingleAsync();
        var disabledIndexes = await context.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM sys.indexes WHERE is_disabled=1")
            .SingleAsync();

        Assert.Equal(0, badForeignKeys);
        Assert.Equal(0, badChecks);
        Assert.Equal(0, disabledIndexes);
    }

    [Fact]
    public async Task PublicStorePage_ReturnsOnlyAnActiveStoreAndNormalizesPage()
    {
        await using var context = CreateContext();
        var store = await context.Stores.AsNoTracking().FirstAsync(item => item.Status == "ACTIVE");
        var result = await new StoresController(context).Details(store.Slug, sort: "invalid", page: 0);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PublicStorePageViewModel>(view.Model);
        Assert.Equal(store.StoreName, model.Store.StoreName);
        Assert.Equal(1, model.Page);
        Assert.All(model.Products, item => Assert.True(item.CurrentPrice > 0));
    }

    [Fact]
    public async Task PublicStorePage_ReturnsNotFoundForNonActiveStore()
    {
        await using var context = CreateContext();
        var store = await context.Stores.AsNoTracking().SingleAsync(item => item.Status != "ACTIVE");
        var result = await new StoresController(context).Details(store.Slug);

        Assert.IsType<NotFoundResult>(result);
    }

    private static ThuongMaiDienTuDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ThuongMaiDienTuDbContext>()
            .UseSqlServer(SqlServerTestConfiguration.GetConnection().ConnectionString)
            .Options);
}
