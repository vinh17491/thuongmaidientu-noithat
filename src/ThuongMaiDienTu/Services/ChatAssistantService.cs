using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ThuongMaiDienTu.Data;
using ThuongMaiDienTu.ViewModels;

namespace ThuongMaiDienTu.Services;

public interface IChatAssistantService
{
    Task<ChatAssistantResponse> ReplyAsync(ChatAssistantRequest request, ClaimsPrincipal user, CancellationToken cancellationToken);
}

public sealed partial class LocalChatAssistantService(ThuongMaiDienTuDbContext context) : IChatAssistantService
{
    private static readonly string[] ProductPrefixes = ["tìm sản phẩm", "tim san pham", "tìm", "tim", "mua", "cho tôi xem", "cho toi xem"];
    private static readonly string[] StorePrefixes = ["tìm gian hàng", "tim gian hang", "gian hàng", "gian hang", "store"];
    private static readonly string[] CategoryPrefixes = ["tìm danh mục", "tim danh muc", "danh mục", "danh muc", "category"];

    public async Task<ChatAssistantResponse> ReplyAsync(ChatAssistantRequest request, ClaimsPrincipal user, CancellationToken token)
    {
        var message = request.Message.Trim();
        var normalized = NormalizeText(message);
        if (normalized is "xin chao" or "chao" or "hello" or "hi" or "bat dau lai")
            return new("Xin chào 👋 Tôi là trợ lý mua sắm của Nội Thất Hub. Tôi có thể giúp gì cho bạn?", "greeting", [], [], [], null,
                ["Xem sản phẩm nổi bật", "Xem sản phẩm khuyến mãi", "Hướng dẫn mua hàng"]);
        if (normalized == "xem san pham noi bat") return await FixedProductsAsync(false, token);
        if (normalized == "xem san pham khuyen mai") return await FixedProductsAsync(true, token);
        if (normalized == "huong dan mua hang")
            return new("Bạn chọn sản phẩm, chọn phiên bản phù hợp, thêm vào giỏ hàng rồi tiến hành đặt hàng. Giá và tồn kho sẽ được kiểm tra lại trước khi xác nhận.",
                "checkout_help", [], [], [], null, ["Xem sản phẩm nổi bật", "Xem sản phẩm khuyến mãi", "Bắt đầu lại"]);
#pragma warning disable CS0162
        if (normalized is "xin chao" or "chao" or "hello" or "hi")
            return new("Xin chào! Tôi có thể tìm sản phẩm, gian hàng, danh mục hoặc hướng dẫn đặt hàng.", "greeting", [], [], [], null, ["Tìm bàn làm việc", "Cách đặt hàng"]);

        var intent = DetectIntent(normalized);
        if (intent == "order_lookup") return await OrderAsync(message, user, token);
        if (intent == "checkout_help") return new("Chọn SKU, thêm vào giỏ rồi thực hiện thanh toán.", intent, [], [], [], null, []);
        if (intent == "shipping_help") return new("Thông tin vận chuyển và trạng thái giao hàng nằm trong chi tiết đơn.", intent, [], [], [], null, []);
        if (intent == "review_help") return new("Bạn có thể đánh giá sản phẩm sau khi đơn đủ điều kiện.", intent, [], [], [], null, []);

        var searchTerm = ExtractSearchTerm(message, intent);
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new("Tôi chưa hiểu yêu cầu. Bạn có thể thử “tìm bàn làm việc” hoặc “tra đơn DH000001”.", "fallback", [], [], [], null, ["Tìm sản phẩm", "Tra cứu đơn hàng"]);

        if (intent == "store_search")
        {
            var stores = await context.Stores.AsNoTracking()
                .Where(x => x.Status == "ACTIVE" && x.StoreName.Contains(searchTerm))
                .OrderBy(x => x.StoreName).Take(5)
                .Select(x => new ChatStoreSuggestion(x.StoreName, x.Slug, "/cua-hang/" + x.Slug))
                .ToListAsync(token);
            return new(stores.Count == 0 ? "Không tìm thấy gian hàng công khai phù hợp." : "Đây là các gian hàng phù hợp.", intent, [], stores, [], null, []);
        }

        if (intent == "category_search")
        {
            var categories = await context.ProductCategories.AsNoTracking()
                .Where(x => x.Status == "ACTIVE" && x.CategoryName.Contains(searchTerm))
                .OrderBy(x => x.SortOrder).Take(5)
                .Select(x => new ChatCategorySuggestion(x.CategoryName, x.Slug, "/danh-muc/" + x.Slug))
                .ToListAsync(token);
            return new(categories.Count == 0 ? "Không tìm thấy danh mục công khai phù hợp." : "Đây là các danh mục phù hợp.", intent, [], [], categories, null, []);
        }

        var now = DateTime.Now;
        var rows = await context.Products.AsNoTracking()
            .Where(x => x.Status == "ACTIVE" && x.Store.Status == "ACTIVE" && x.Category.Status == "ACTIVE"
                && x.ProductSkus.Any(s => s.Status == "ACTIVE")
                && (x.ProductName.Contains(searchTerm) || x.Store.StoreName.Contains(searchTerm)
                    || x.Category.CategoryName.Contains(searchTerm) || (x.Brand != null && x.Brand.Contains(searchTerm))))
            .OrderBy(x => x.ProductName).Take(5)
            .Select(x => new
            {
                x.ProductName, x.Slug,
                StoreName = x.Store.StoreName, StoreSlug = x.Store.Slug,
                CategoryName = x.Category.CategoryName, CategorySlug = x.Category.Slug,
                Sku = x.ProductSkus.Where(s => s.Status == "ACTIVE").OrderBy(s => s.SkuId)
                    .Select(s => new { s.Price, s.SalePrice, s.SaleStart, s.SaleEnd, s.StockQuantity }).First()
            }).ToListAsync(token);

        var products = rows.Select(x =>
        {
            var price = ProductPricingHelper.Calculate(x.Sku.Price, x.Sku.SalePrice, x.Sku.SaleStart, x.Sku.SaleEnd, now);
            return new ChatProductSuggestion(x.ProductName, x.Slug, price.CurrentPrice, price.IsOnSale ? x.Sku.Price : null,
                x.StoreName, x.StoreSlug, x.CategoryName, x.CategorySlug, x.Sku.StockQuantity > 0, "/san-pham/" + x.Slug);
        }).ToList();
        return new(products.Count == 0 ? "Không tìm thấy sản phẩm công khai phù hợp." : "Tôi tìm thấy sản phẩm phù hợp.", "product_search", products, [], [], null, []);
    }

#pragma warning restore CS0162

    private async Task<ChatAssistantResponse> FixedProductsAsync(bool onSale, CancellationToken token)
    {
        var fixedSlugs = onSale
            ? new[] { "chao-chong-dinh-28cm", "noi-com-dien-sharp-18l", "may-xay-sinh-to-philips" }
            : new[] { "ghe-an-boc-ni", "ke-sach-go-ba-tang", "ban-lam-viec-go-soi" };
        var now = DateTime.Now;
        var rows = await context.Products.AsNoTracking()
            .Where(x => fixedSlugs.Contains(x.Slug) && x.Status == "ACTIVE" && x.Store.Status == "ACTIVE"
                && x.Category.Status == "ACTIVE" && x.ProductSkus.Any(s => s.Status == "ACTIVE"))
            .Select(x => new
            {
                x.ProductName, x.Slug, StoreName = x.Store.StoreName, StoreSlug = x.Store.Slug,
                CategoryName = x.Category.CategoryName, CategorySlug = x.Category.Slug,
                Sku = x.ProductSkus.Where(s => s.Status == "ACTIVE").OrderBy(s => s.SkuId)
                    .Select(s => new { s.Price, s.SalePrice, s.SaleStart, s.SaleEnd, s.StockQuantity }).First()
            }).ToListAsync(token);
        var products = rows.OrderBy(x => Array.IndexOf(fixedSlugs, x.Slug)).Select(x =>
        {
            var price = ProductPricingHelper.Calculate(x.Sku.Price, x.Sku.SalePrice, x.Sku.SaleStart, x.Sku.SaleEnd, now);
            return new ChatProductSuggestion(x.ProductName, x.Slug, price.CurrentPrice, price.IsOnSale ? x.Sku.Price : null,
                x.StoreName, x.StoreSlug, x.CategoryName, x.CategorySlug, x.Sku.StockQuantity > 0, "/san-pham/" + x.Slug);
        }).ToList();
        return new(onSale ? "Đây là 3 sản phẩm khuyến mãi dành cho bạn." : "Đây là 3 sản phẩm nổi bật dành cho bạn.",
            onSale ? "fixed_sale_products" : "fixed_featured_products", products, [], [], null,
            [onSale ? "Xem sản phẩm nổi bật" : "Xem sản phẩm khuyến mãi", "Hướng dẫn mua hàng", "Bắt đầu lại"]);
    }

    public static string ExtractSearchTerm(string message, string intent)
    {
        var prefixes = intent switch
        {
            "store_search" => StorePrefixes,
            "category_search" => CategoryPrefixes,
            _ => ProductPrefixes
        };
        var result = message.Trim();
        var normalized = NormalizeText(result);
        foreach (var prefix in prefixes.OrderByDescending(x => x.Length))
        {
            if (!normalized.StartsWith(prefix + " ", StringComparison.Ordinal) && normalized != prefix) continue;
            var wordCount = prefix.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            result = string.Join(' ', result.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(wordCount));
            break;
        }
        return result.Trim(' ', '?', '.', ',', '!');
    }

    public static string NormalizeText(string value)
    {
        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) != System.Globalization.UnicodeCategory.NonSpacingMark)
                builder.Append(character == 'đ' ? 'd' : character);
        return MultipleWhitespace().Replace(builder.ToString().Normalize(NormalizationForm.FormC), " ");
    }

    private static string DetectIntent(string normalized) =>
        normalized.Contains("don ") || normalized == "don" || normalized.Contains("order") || OrderCode().IsMatch(normalized) ? "order_lookup" :
        normalized.Contains("gian hang") || normalized.Contains("store") ? "store_search" :
        normalized.Contains("danh muc") || normalized.Contains("category") ? "category_search" :
        normalized.Contains("dat hang") || normalized.Contains("checkout") || normalized.Contains("thanh toan") ? "checkout_help" :
        normalized.Contains("van chuyen") || normalized.Contains("giao hang") ? "shipping_help" :
        normalized.Contains("danh gia") || normalized.Contains("review") ? "review_help" : "product_search";

    private async Task<ChatAssistantResponse> OrderAsync(string message, ClaimsPrincipal user, CancellationToken token)
    {
        if (!long.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return new("Bạn cần đăng nhập để tra cứu đơn hàng.", "order_lookup", [], [], [], null, ["Đăng nhập"]);

        var code = OrderCode().Match(message).Value;
        var query = context.Orders.AsNoTracking().Where(x => x.UserId == userId);
        if (!string.IsNullOrWhiteSpace(code)) query = query.Where(x => x.OrderCode == code.ToUpper());
        var order = await query.OrderByDescending(x => x.CreatedAt)
            .Select(x => new ChatOrderSummary(x.OrderCode, x.CreatedAt, x.OrderStatus, x.TotalAmount))
            .FirstOrDefaultAsync(token);
        return new(order is null ? "Không tìm thấy hoặc không có quyền truy cập đơn hàng." :
            $"Đơn {order.OrderCode}, trạng thái {order.Status}, tổng {order.Total:N0} đ.", "order_lookup", [], [], [], order, []);
    }

    [GeneratedRegex(@"\bDH\d{6,}\b", RegexOptions.IgnoreCase)]
    private static partial Regex OrderCode();

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultipleWhitespace();
}
