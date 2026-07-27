namespace ThuongMaiDienTu.Services;

public enum PromotionState
{
    None,
    Upcoming,
    Active,
    Ended
}

public sealed record ProductPriceInfo(
    decimal CurrentPrice,
    bool IsOnSale,
    int DiscountPercent,
    PromotionState PromotionState)
{
    public string PromotionLabel => PromotionState switch
    {
        PromotionState.Upcoming => "Sắp diễn ra",
        PromotionState.Active => "Đang khuyến mãi",
        PromotionState.Ended => "Đã kết thúc",
        _ => "Không khuyến mãi"
    };
}

public static class ProductPricingHelper
{
    public static ProductPriceInfo Calculate(
        decimal price,
        decimal? salePrice,
        DateTime? saleStart,
        DateTime? saleEnd,
        DateTime now)
    {
        if (!salePrice.HasValue ||
            salePrice.Value <= 0 ||
            salePrice.Value >= price)
        {
            return new ProductPriceInfo(price, false, 0, PromotionState.None);
        }

        if (saleStart.HasValue && saleStart.Value > now)
        {
            return new ProductPriceInfo(price, false, 0, PromotionState.Upcoming);
        }

        if (saleEnd.HasValue && saleEnd.Value < now)
        {
            return new ProductPriceInfo(price, false, 0, PromotionState.Ended);
        }

        var discountPercent = price > 0
            ? (int)Math.Round(
                (price - salePrice.Value) * 100m / price,
                0,
                MidpointRounding.AwayFromZero)
            : 0;

        return new ProductPriceInfo(
            salePrice.Value,
            true,
            discountPercent,
            PromotionState.Active);
    }
}
