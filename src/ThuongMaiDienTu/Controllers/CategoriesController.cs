using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuongMaiDienTu.Data;
using ThuongMaiDienTu.ViewModels;

namespace ThuongMaiDienTu.Controllers;

[Route("danh-muc")]
public sealed class CategoriesController(ThuongMaiDienTuDbContext context) : Controller
{
    [HttpGet("{categorySlug}")]
    public async Task<IActionResult> Details(string categorySlug, CancellationToken cancellationToken = default)
    {
        var category = await context.ProductCategories.AsNoTracking().SingleOrDefaultAsync(item => item.Slug == categorySlug && item.Status == "ACTIVE", cancellationToken);
        if (category is null) return NotFound();
        var result = await new ProductsController(context).Index(new ProductCatalogQuery { CategoryId = category.CategoryId }, cancellationToken);
        if (result is not ViewResult view || view.Model is not ProductCatalogViewModel model) return NotFound();
        ViewData["Title"] = category.CategoryName;
        ViewData["Description"] = category.Description ?? $"Sản phẩm trong danh mục {category.CategoryName}.";
        ViewData["CanonicalPath"] = $"/danh-muc/{category.Slug}";
        ViewData["BreadcrumbName"] = category.CategoryName;
        ViewData["BreadcrumbPath"] = $"/danh-muc/{category.Slug}";
        return View("~/Views/Products/Index.cshtml", model);
    }
}
