using System.ComponentModel.DataAnnotations;

namespace ThuongMaiDienTu.ViewModels;

public class CartViewModel
{
    public IReadOnlyList<CartItemViewModel> Items { get; set; } = [];

    public int TotalQuantity => Items.Sum(item => item.Quantity);

    public decimal Subtotal => Items.Sum(item => item.LineTotal);

    public bool IsEmpty => Items.Count == 0;
}

public class CartItemViewModel
{
    public long CartItemId { get; set; }

    public long SkuId { get; set; }

    public long ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SkuCode { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public string? AltText { get; set; }

    public int Quantity { get; set; }

    public int StockQuantity { get; set; }

    public decimal Price { get; set; }

    public decimal? SalePrice { get; set; }

    public decimal CurrentPrice { get; set; }

    public bool IsOnSale { get; set; }

    public int DiscountPercent { get; set; }

    public decimal LineTotal => CurrentPrice * Quantity;

    public bool IsAvailable { get; set; }

    public bool CanUpdateQuantity { get; set; }

    public string AvailabilityMessage { get; set; } = string.Empty;
}

public class AddToCartViewModel
{
    [Range(1, long.MaxValue, ErrorMessage = "Sản phẩm không hợp lệ.")]
    public long SkuId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
    public int Quantity { get; set; } = 1;
}

public class UpdateCartItemViewModel
{
    [Range(1, long.MaxValue, ErrorMessage = "Dòng giỏ hàng không hợp lệ.")]
    public long CartItemId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0. Hãy dùng nút Xóa để bỏ sản phẩm.")]
    public int Quantity { get; set; }
}
