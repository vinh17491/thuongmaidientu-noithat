using ThuongMaiDienTu.Services;
using Xunit;

namespace ThuongMaiDienTu.Tests;

public sealed class MarketplaceCoreTests
{
    [Fact]
    public void Pricing_UsesActivePromotion()
    {
        var now = new DateTime(2026, 7, 29, 12, 0, 0, DateTimeKind.Utc);
        var result = ProductPricingHelper.Calculate(
            100_000m, 80_000m, now.AddDays(-1), now.AddDays(1), now);

        Assert.True(result.IsOnSale);
        Assert.Equal(80_000m, result.CurrentPrice);
        Assert.Equal(20, result.DiscountPercent);
    }

    [Fact]
    public void Pricing_IgnoresExpiredPromotion()
    {
        var now = new DateTime(2026, 7, 29, 12, 0, 0, DateTimeKind.Utc);
        var result = ProductPricingHelper.Calculate(
            100_000m, 80_000m, now.AddDays(-3), now.AddDays(-1), now);

        Assert.False(result.IsOnSale);
        Assert.Equal(100_000m, result.CurrentPrice);
    }

    [Theory]
    [InlineData("PENDING", "CONFIRMED")]
    [InlineData("PENDING", "CANCELLED")]
    [InlineData("CONFIRMED", "PROCESSING")]
    [InlineData("PROCESSING", "SHIPPING")]
    [InlineData("SHIPPING", "DELIVERED")]
    public void StoreOrder_AllowsDocumentedTransitions(string current, string next) =>
        Assert.True(StoreOrderWorkflow.CanTransition(current, next));

    [Theory]
    [InlineData("PENDING", "DELIVERED")]
    [InlineData("DELIVERED", "PROCESSING")]
    [InlineData("CANCELLED", "CONFIRMED")]
    public void StoreOrder_RejectsInvalidTransitions(string current, string next) =>
        Assert.False(StoreOrderWorkflow.CanTransition(current, next));

    [Fact]
    public void StoreOrder_RejectsCancellationAfterPickup() =>
        Assert.False(StoreOrderWorkflow.CanTransition(
            StoreOrderWorkflow.Confirmed,
            StoreOrderWorkflow.Cancelled,
            carrierHasPickedUp: true));

    [Theory]
    [InlineData("CREATED", "READY_FOR_PICKUP")]
    [InlineData("READY_FOR_PICKUP", "PICKED_UP")]
    [InlineData("PICKED_UP", "IN_TRANSIT")]
    [InlineData("IN_TRANSIT", "DELIVERED")]
    public void Shipment_AllowsDocumentedTransitions(string current, string next) =>
        Assert.True(ShipmentWorkflow.CanTransition(current, next));

    [Theory]
    [InlineData("CREATED", "DELIVERED")]
    [InlineData("DELIVERED", "IN_TRANSIT")]
    [InlineData("CANCELLED", "READY_FOR_PICKUP")]
    public void Shipment_RejectsInvalidTransitions(string current, string next) =>
        Assert.False(ShipmentWorkflow.CanTransition(current, next));

    [Theory]
    [InlineData(new[] { "PENDING", "PENDING" }, "PENDING")]
    [InlineData(new[] { "CONFIRMED", "PENDING" }, "CONFIRMED")]
    [InlineData(new[] { "DELIVERED", "CANCELLED" }, "DELIVERED")]
    [InlineData(new[] { "CANCELLED", "CANCELLED" }, "CANCELLED")]
    public void ParentStatus_IsAggregated(string[] statuses, string expected) =>
        Assert.Equal(expected, StoreOrderWorkflow.AggregateParentStatus(statuses));

    [Fact]
    public void StoreOrderTotal_IncludesShippingAndDiscount() =>
        Assert.Equal(
            120_000m,
            StoreOrderWorkflow.CalculateTotal(100_000m, 10_000m, 30_000m));

    [Fact]
    public void ParentTotal_ExcludesCancelledPartFromMixedOrder()
    {
        var total = StoreOrderWorkflow.CalculatePayableParentTotal(
        [
            (StoreOrderWorkflow.Delivered, 150_000m),
            (StoreOrderWorkflow.Cancelled, 90_000m)
        ]);

        Assert.Equal(150_000m, total);
        Assert.Equal(
            StoreOrderWorkflow.Delivered,
            StoreOrderWorkflow.AggregateParentStatus(
                [StoreOrderWorkflow.Delivered, StoreOrderWorkflow.Cancelled]));
    }

    [Theory]
    [InlineData("PENDING", "ACTIVE")]
    [InlineData("PENDING", "REJECTED")]
    [InlineData("ACTIVE", "SUSPENDED")]
    [InlineData("SUSPENDED", "ACTIVE")]
    [InlineData("REJECTED", "PENDING")]
    public void StoreWorkflow_AllowsDocumentedTransitions(string current, string next) =>
        Assert.True(StoreWorkflow.CanTransition(current, next));

    [Theory]
    [InlineData("ACTIVE", "PENDING")]
    [InlineData("REJECTED", "ACTIVE")]
    [InlineData("SUSPENDED", "REJECTED")]
    public void StoreWorkflow_RejectsUndocumentedTransitions(string current, string next) =>
        Assert.False(StoreWorkflow.CanTransition(current, next));

    [Theory]
    [InlineData("Gian hàng Nội Thất", "gian-hang-noi-that")]
    [InlineData("  Café & Decor  ", "cafe-decor")]
    [InlineData("../Unsafe Path", "unsafe-path")]
    public void StoreSlug_IsSafeAndDiacriticFree(string value, string expected) =>
        Assert.Equal(expected, StoreSlugService.Normalize(value));
}
