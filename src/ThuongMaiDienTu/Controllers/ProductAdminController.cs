using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ThuongMaiDienTu.Data;
using ThuongMaiDienTu.Models;
using ThuongMaiDienTu.Services;
using ThuongMaiDienTu.ViewModels;

namespace ThuongMaiDienTu.Controllers;

[Authorize(Roles = "ADMIN,SELLER")]
public class ProductAdminController : Controller
{
    private static readonly string[] ProductStatuses = ["ACTIVE", "HIDDEN", "DRAFT"];
    private static readonly string[] SkuStatuses = ["ACTIVE", "INACTIVE"];
    private readonly ThuongMaiDienTuDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IStoreOwnershipService _ownership;
    private readonly IWebHostEnvironment _environment;

    public ProductAdminController(
        ThuongMaiDienTuDbContext context,
        ICurrentUserService currentUser,
        IStoreOwnershipService ownership,
        IWebHostEnvironment environment)
    {
        _context = context;
        _currentUser = currentUser;
        _ownership = ownership;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? searchTerm,
        long? categoryId,
        string? status)
    {
        searchTerm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();
        status = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToUpperInvariant();

        var query = _ownership.ScopeProducts(_context.Products
            .Include(product => product.Category)
            .Include(product => product.Store)
            .Include(product => product.ProductSkus)
            .Include(product => product.ProductImages)
            .AsNoTracking()
            .AsQueryable());

        if (searchTerm is not null)
        {
            query = query.Where(product =>
                product.ProductName.Contains(searchTerm) ||
                product.ProductSkus.Any(sku => sku.SkuCode.Contains(searchTerm)));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == categoryId.Value);
        }

        if (status is not null && ProductStatuses.Contains(status))
        {
            query = query.Where(product => product.Status == status);
        }

        var products = await query
            .OrderByDescending(product => product.CreatedAt)
            .ThenBy(product => product.ProductName)
            .ToListAsync();

        var now = DateTime.Now;
        var model = new ProductAdminIndexViewModel
        {
            SearchTerm = searchTerm,
            CategoryId = categoryId,
            Status = status,
            Categories = await GetCategoryOptionsAsync(),
            Items = products.Select(product =>
            {
                var sku = product.ProductSkus.OrderBy(item => item.SkuId).FirstOrDefault();
                var priceInfo = sku is null
                    ? null
                    : ProductPricingHelper.Calculate(
                        sku.Price,
                        sku.SalePrice,
                        sku.SaleStart,
                        sku.SaleEnd,
                        now);

                return new ProductAdminListItemViewModel
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    CategoryName = product.Category.CategoryName,
                    StoreName = product.Store.StoreName,
                    SkuCode = sku?.SkuCode,
                    Price = sku?.Price,
                    SalePrice = sku?.SalePrice,
                    CurrentPrice = priceInfo?.CurrentPrice,
                    PromotionStatus = priceInfo?.PromotionLabel ?? "Không khuyến mãi",
                    SaleStart = sku?.SaleStart,
                    SaleEnd = sku?.SaleEnd,
                    StockQuantity = sku?.StockQuantity,
                    Status = product.Status,
                    ImageUrl = product.ProductImage?.ImageUrl,
                    CreatedAt = product.CreatedAt
                };
            }).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new ProductAdminFormViewModel();
        await LoadFormOptionsAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductAdminFormViewModel model)
    {
        Normalize(model);
        await PrepareImageAsync(model);

        var store = _currentUser.IsAdmin
            ? await _context.Stores.AsNoTracking()
                .Where(item => item.Status == "ACTIVE")
                .OrderBy(item => item.StoreId)
                .FirstOrDefaultAsync()
            : await _ownership.GetActiveOwnedStoreAsync();

        await ValidateFormAsync(model, store?.StoreId);

        if (!ModelState.IsValid || store is null)
        {
            if (store is null)
            {
                ModelState.AddModelError(string.Empty, "Không tìm thấy cửa hàng đang hoạt động để tạo sản phẩm.");
            }

            await LoadFormOptionsAsync();
            return View(model);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var product = new Product
            {
                StoreId = store.StoreId,
                CategoryId = model.CategoryId,
                ProductName = model.ProductName,
                Slug = model.Slug,
                Brand = model.Brand,
                ShortDescription = model.ShortDescription,
                Description = model.Description,
                Status = model.ProductStatus,
                SeoTitle = model.SeoTitle,
                SeoDescription = model.SeoDescription,
                CreatedAt = DateTime.Now
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var sku = new ProductSku
            {
                ProductId = product.ProductId,
                SkuCode = model.SkuCode,
                Price = model.Price,
                SalePrice = model.SalePrice,
                SaleStart = model.SaleStart,
                SaleEnd = model.SaleEnd,
                StockQuantity = model.StockQuantity,
                Status = model.SkuStatus
            };

            _context.ProductSkus.Add(sku);

            if (model.ImageUrl is not null)
            {
                _context.ProductImages.Add(new ProductImage
                {
                    ProductId = product.ProductId,
                    ImageUrl = model.ImageUrl,
                    AltText = model.AltText,
                    IsPrimary = true,
                    SortOrder = 0
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] = "Thêm sản phẩm thành công.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(
                string.Empty,
                "Không thể lưu sản phẩm. Slug hoặc mã SKU có thể vừa được sử dụng.");
            await LoadFormOptionsAsync();
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var product = await _ownership.ScopeProducts(_context.Products)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.ProductId == id.Value);

        if (product is null)
        {
            return NotFound();
        }

        var sku = await _context.ProductSkus
            .AsNoTracking()
            .Where(item => item.ProductId == product.ProductId)
            .OrderBy(item => item.SkuId)
            .FirstOrDefaultAsync();

        var image = await _context.ProductImages
            .AsNoTracking()
            .Where(item => item.ProductId == product.ProductId)
            .OrderByDescending(item => item.IsPrimary)
            .ThenBy(item => item.ImageId)
            .FirstOrDefaultAsync();

        var model = new ProductAdminFormViewModel
        {
            ProductId = product.ProductId,
            SkuId = sku?.SkuId,
            ImageId = image?.ImageId,
            ProductName = product.ProductName,
            CategoryId = product.CategoryId,
            Slug = product.Slug,
            Brand = product.Brand,
            ShortDescription = product.ShortDescription,
            Description = product.Description,
            SeoTitle = product.SeoTitle,
            SeoDescription = product.SeoDescription,
            ProductStatus = product.Status,
            SkuCode = sku?.SkuCode ?? string.Empty,
            Price = sku?.Price ?? 0,
            SalePrice = sku?.SalePrice,
            SaleStart = sku?.SaleStart,
            SaleEnd = sku?.SaleEnd,
            StockQuantity = sku?.StockQuantity ?? 0,
            SkuStatus = sku?.Status ?? "ACTIVE",
            ImageUrl = image?.ImageUrl,
            AltText = image?.AltText
        };

        await LoadFormOptionsAsync(product.StoreId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, ProductAdminFormViewModel model)
    {
        if (model.ProductId != id)
        {
            return NotFound();
        }

        Normalize(model);
        await PrepareImageAsync(model);

        var product = await _ownership.ScopeProducts(_context.Products)
            .FirstOrDefaultAsync(item => item.ProductId == id);

        if (product is null)
        {
            return NotFound();
        }

        if (!_currentUser.IsAdmin && !await _context.Stores.AnyAsync(store =>
                store.StoreId == product.StoreId && store.Status == "ACTIVE"))
        {
            ModelState.AddModelError(string.Empty, "Chỉ gian hàng ACTIVE mới được quản lý sản phẩm.");
            await LoadFormOptionsAsync(product.StoreId);
            return View(model);
        }

        var sku = await _context.ProductSkus
            .Where(item => item.ProductId == id)
            .OrderBy(item => item.SkuId)
            .FirstOrDefaultAsync();

        var image = await _context.ProductImages
            .Where(item => item.ProductId == id)
            .OrderByDescending(item => item.IsPrimary)
            .ThenBy(item => item.ImageId)
            .FirstOrDefaultAsync();

        if ((sku is not null && model.SkuId != sku.SkuId) ||
            (image is not null && model.ImageId != image.ImageId))
        {
            return NotFound();
        }

        await ValidateFormAsync(model, product.StoreId, product.ProductId, sku?.SkuId);

        if (!ModelState.IsValid)
        {
            await LoadFormOptionsAsync(product.StoreId);
            return View(model);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            product.CategoryId = model.CategoryId;
            product.ProductName = model.ProductName;
            product.Slug = model.Slug;
            product.Brand = model.Brand;
            product.ShortDescription = model.ShortDescription;
            product.Description = model.Description;
            product.Status = model.ProductStatus;
            product.SeoTitle = model.SeoTitle;
            product.SeoDescription = model.SeoDescription;
            product.UpdatedAt = DateTime.Now;

            if (sku is null)
            {
                sku = new ProductSku { ProductId = product.ProductId };
                _context.ProductSkus.Add(sku);
            }

            sku.SkuCode = model.SkuCode;
            sku.Price = model.Price;
            sku.SalePrice = model.SalePrice;
            sku.SaleStart = model.SaleStart;
            sku.SaleEnd = model.SaleEnd;
            sku.StockQuantity = model.StockQuantity;
            sku.Status = model.SkuStatus;

            if (model.ImageUrl is null)
            {
                if (image is not null)
                {
                    _context.ProductImages.Remove(image);
                }
            }
            else if (image is null)
            {
                _context.ProductImages.Add(new ProductImage
                {
                    ProductId = product.ProductId,
                    ImageUrl = model.ImageUrl,
                    AltText = model.AltText,
                    IsPrimary = true,
                    SortOrder = 0
                });
            }
            else
            {
                image.ImageUrl = model.ImageUrl;
                image.AltText = model.AltText;
                image.IsPrimary = true;
                image.SortOrder = 0;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(
                string.Empty,
                "Không thể cập nhật sản phẩm. Vui lòng kiểm tra lại slug và mã SKU.");
            await LoadFormOptionsAsync(product.StoreId);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(long? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var product = await _ownership.ScopeProducts(_context.Products
            .Include(item => item.Category)
            .Include(item => item.Store)
            .Include(item => item.ProductSkus)
            .Include(item => item.ProductImages)
            .AsNoTracking())
            .FirstOrDefaultAsync(item => item.ProductId == id.Value);

        if (product is null)
        {
            return NotFound();
        }

        var sku = product.ProductSkus.OrderBy(item => item.SkuId).FirstOrDefault();
        var priceInfo = sku is null
            ? null
            : ProductPricingHelper.Calculate(
                sku.Price,
                sku.SalePrice,
                sku.SaleStart,
                sku.SaleEnd,
                DateTime.Now);
        var hasOrderItems = sku is not null &&
                            await _context.OrderItems.AsNoTracking()
                                .AnyAsync(item => item.SkuId == sku.SkuId);

        var model = new ProductAdminDetailsViewModel
        {
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            CategoryName = product.Category.CategoryName,
            StoreName = product.Store.StoreName,
            Slug = product.Slug,
            Brand = product.Brand,
            ShortDescription = product.ShortDescription,
            Description = product.Description,
            SeoTitle = product.SeoTitle,
            SeoDescription = product.SeoDescription,
            ProductStatus = product.Status,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            SkuId = sku?.SkuId,
            SkuCode = sku?.SkuCode,
            Price = sku?.Price,
            SalePrice = sku?.SalePrice,
            CurrentPrice = priceInfo?.CurrentPrice,
            PromotionStatus = priceInfo?.PromotionLabel ?? "Không khuyến mãi",
            SaleStart = sku?.SaleStart,
            SaleEnd = sku?.SaleEnd,
            StockQuantity = sku?.StockQuantity,
            SkuStatus = sku?.Status,
            ImageUrl = product.ProductImage?.ImageUrl,
            AltText = product.ProductImage?.AltText,
            HasOrderItems = hasOrderItems
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(long id)
    {
        var product = await _ownership.ScopeProducts(_context.Products)
            .FirstOrDefaultAsync(item => item.ProductId == id);

        if (product is null)
        {
            return NotFound();
        }

        if (!_currentUser.IsAdmin && !await _context.Stores.AnyAsync(store =>
                store.StoreId == product.StoreId && store.Status == "ACTIVE"))
        {
            return NotFound();
        }

        var sku = await _context.ProductSkus
            .Where(item => item.ProductId == id)
            .OrderBy(item => item.SkuId)
            .FirstOrDefaultAsync();

        var isActive = product.Status == "ACTIVE";
        product.Status = isActive ? "HIDDEN" : "ACTIVE";
        product.UpdatedAt = DateTime.Now;

        if (sku is not null)
        {
            sku.Status = isActive ? "INACTIVE" : "ACTIVE";
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = isActive
            ? "Đã ẩn sản phẩm."
            : "Đã mở bán sản phẩm.";

        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateFormAsync(
        ProductAdminFormViewModel model,
        long? storeId,
        long? excludedProductId = null,
        long? excludedSkuId = null)
    {
        if (!ProductStatuses.Contains(model.ProductStatus))
        {
            ModelState.AddModelError(
                nameof(model.ProductStatus),
                "Trạng thái sản phẩm không hợp lệ.");
        }

        if (!SkuStatuses.Contains(model.SkuStatus))
        {
            ModelState.AddModelError(
                nameof(model.SkuStatus),
                "Trạng thái SKU không hợp lệ.");
        }

        var categoryExists = await _context.ProductCategories
            .AsNoTracking()
            .AnyAsync(category =>
                category.CategoryId == model.CategoryId &&
                category.Status == "ACTIVE");

        if (!categoryExists)
        {
            ModelState.AddModelError(
                nameof(model.CategoryId),
                "Danh mục không tồn tại hoặc đã ngừng hoạt động.");
        }

        if (!storeId.HasValue ||
            !await _context.Stores.AsNoTracking()
                .AnyAsync(store => store.StoreId == storeId.Value && store.Status == "ACTIVE"))
        {
            ModelState.AddModelError(string.Empty, "Cửa hàng không tồn tại hoặc đã ngừng hoạt động.");
        }

        if (storeId.HasValue)
        {
            var slugExists = await _context.Products
                .AsNoTracking()
                .AnyAsync(product =>
                    product.StoreId == storeId.Value &&
                    product.Slug == model.Slug &&
                    (!excludedProductId.HasValue || product.ProductId != excludedProductId.Value));

            if (slugExists)
            {
                ModelState.AddModelError(nameof(model.Slug), "Slug này đã được sử dụng trong cửa hàng.");
            }
        }

        var skuCodeExists = await _context.ProductSkus
            .AsNoTracking()
            .AnyAsync(sku =>
                sku.SkuCode == model.SkuCode &&
                (!excludedSkuId.HasValue || sku.SkuId != excludedSkuId.Value));

        if (skuCodeExists)
        {
            ModelState.AddModelError(nameof(model.SkuCode), "Mã SKU này đã được sử dụng.");
        }
    }

    private async Task LoadFormOptionsAsync(long? storeId = null)
    {
        ViewBag.Categories = await GetCategoryOptionsAsync();
        ViewBag.ProductStatuses = ProductStatuses
            .Select(status => new SelectListItem(GetProductStatusLabel(status), status))
            .ToList();
        ViewBag.SkuStatuses = SkuStatuses
            .Select(status => new SelectListItem(
                status == "ACTIVE" ? "Hoạt động" : "Ngừng hoạt động",
                status))
            .ToList();

        var storeQuery = _context.Stores.AsNoTracking().AsQueryable();
        if (storeId.HasValue)
        {
            storeQuery = storeQuery.Where(store => store.StoreId == storeId.Value);
        }
        else
        {
            storeQuery = storeQuery.Where(store => store.Status == "ACTIVE");
        }

        ViewBag.StoreName = await storeQuery
            .OrderBy(store => store.StoreId)
            .Select(store => store.StoreName)
            .FirstOrDefaultAsync() ?? "Không có cửa hàng hoạt động";
    }

    private async Task<IReadOnlyList<SelectListItem>> GetCategoryOptionsAsync()
    {
        return await _context.ProductCategories
            .AsNoTracking()
            .Where(category => category.Status == "ACTIVE")
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.CategoryName)
            .Select(category => new SelectListItem(
                category.CategoryName,
                category.CategoryId.ToString()))
            .ToListAsync();
    }

    private static void Normalize(ProductAdminFormViewModel model)
    {
        model.ProductName = model.ProductName?.Trim() ?? string.Empty;
        model.Slug = model.Slug?.Trim().ToLowerInvariant() ?? string.Empty;
        model.Brand = TrimToNull(model.Brand);
        model.ShortDescription = TrimToNull(model.ShortDescription);
        model.Description = TrimToNull(model.Description);
        model.SeoTitle = TrimToNull(model.SeoTitle);
        model.SeoDescription = TrimToNull(model.SeoDescription);
        model.ProductStatus = model.ProductStatus?.Trim().ToUpperInvariant() ?? string.Empty;
        model.SkuCode = model.SkuCode?.Trim().ToUpperInvariant() ?? string.Empty;
        model.SkuStatus = model.SkuStatus?.Trim().ToUpperInvariant() ?? string.Empty;
        model.ImageUrl = TrimToNull(model.ImageUrl);
        model.AltText = TrimToNull(model.AltText);
    }

    private async Task PrepareImageAsync(ProductAdminFormViewModel model)
    {
        if (model.ImageFile is null || model.ImageFile.Length == 0) return;

        const long maxBytes = 5 * 1024 * 1024;
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var extension = Path.GetExtension(model.ImageFile.FileName).ToLowerInvariant();
        if (model.ImageFile.Length > maxBytes)
        {
            ModelState.AddModelError(nameof(model.ImageFile), "Ảnh không được vượt quá 5 MB.");
            return;
        }

        if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.ImageFile), "Chỉ nhận ảnh JPG, PNG, WEBP hoặc GIF.");
            return;
        }

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var temporaryPath = string.Empty;
        try
        {
            await using var input = model.ImageFile.OpenReadStream();
            var signature = new byte[12];
            var signatureLength = await input.ReadAsync(signature);
            if (!HasValidImageSignature(extension, signature.AsSpan(0, signatureLength)))
            {
                ModelState.AddModelError(nameof(model.ImageFile), "Nội dung file không đúng định dạng ảnh đã chọn.");
                return;
            }

            var webRoot = string.IsNullOrWhiteSpace(_environment.WebRootPath)
                ? Path.Combine(_environment.ContentRootPath, "wwwroot")
                : _environment.WebRootPath;
            var uploadDirectory = Path.GetFullPath(Path.Combine(webRoot, "images", "uploads", "products"));
            Directory.CreateDirectory(uploadDirectory);

            var physicalPath = Path.Combine(uploadDirectory, fileName);
            temporaryPath = physicalPath + ".uploading";
            await using (var output = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await output.WriteAsync(signature.AsMemory(0, signatureLength));
                await input.CopyToAsync(output);
                await output.FlushAsync();
            }

            System.IO.File.Move(temporaryPath, physicalPath);
            temporaryPath = string.Empty;
            model.ImageUrl = $"/images/uploads/products/{fileName}";
            model.ImageSource = "file";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            if (!string.IsNullOrEmpty(temporaryPath))
            {
                try { System.IO.File.Delete(temporaryPath); } catch (IOException) { }
            }

            ModelState.AddModelError(
                nameof(model.ImageFile),
                "Không thể lưu ảnh lên máy chủ. Vui lòng thử lại hoặc chọn ảnh khác.");
        }
    }

    private static bool HasValidImageSignature(string extension, ReadOnlySpan<byte> bytes)
    {
        return extension switch
        {
            ".jpg" or ".jpeg" => bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
            ".png" => bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".gif" => bytes.Length >= 6 &&
                      (bytes[..6].SequenceEqual("GIF87a"u8) || bytes[..6].SequenceEqual("GIF89a"u8)),
            ".webp" => bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8),
            _ => false
        };
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string GetProductStatusLabel(string status)
    {
        return status switch
        {
            "ACTIVE" => "Đang bán",
            "HIDDEN" => "Đang ẩn",
            "DRAFT" => "Bản nháp",
            _ => status
        };
    }
}
