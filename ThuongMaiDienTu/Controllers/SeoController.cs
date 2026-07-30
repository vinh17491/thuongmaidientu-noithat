using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuongMaiDienTu.Data;
using ThuongMaiDienTu.Services;

namespace ThuongMaiDienTu.Controllers;

public sealed class SeoController(ThuongMaiDienTuDbContext context, IConfiguration configuration) : Controller
{
    private readonly SeoUrlService urlService = new(configuration);
    [HttpGet("/sitemap.xml")]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken)
    {
        var urls = new List<string> { urlService.Build("/"), urlService.Build("/san-pham") };
        urls.AddRange((await context.CategoriesForSitemap(cancellationToken)).Select(urlService.Build));
        urls.AddRange((await context.Stores.AsNoTracking().Where(item => item.Status == "ACTIVE").Select(item => item.Slug).ToListAsync(cancellationToken)).Select(slug => urlService.Build("/cua-hang/" + Uri.EscapeDataString(slug))));
        urls.AddRange((await context.Products.AsNoTracking().Where(item => item.Status == "ACTIVE" && item.Store.Status == "ACTIVE" && item.Category.Status == "ACTIVE" && item.ProductSkus.Any(sku => sku.Status == "ACTIVE")).Select(item => item.Slug).ToListAsync(cancellationToken)).Select(slug => urlService.Build("/san-pham/" + Uri.EscapeDataString(slug))));
        XNamespace sitemap = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var document = new XDocument(new XElement(sitemap + "urlset",
            urls.Distinct().Select(url => new XElement(sitemap + "url", new XElement(sitemap + "loc", url)))));
        return Content(document.ToString(SaveOptions.DisableFormatting), "application/xml; charset=utf-8");
    }

    [HttpGet("/robots.txt")]
    public IActionResult Robots() => Content("User-agent: *\nAllow: /\nDisallow: /Admin\nDisallow: /Seller\nDisallow: /Carrier\nDisallow: /Account\nDisallow: /Cart\nDisallow: /Checkout\nDisallow: /Orders\n" + (urlService.HasValidBaseUrl ? $"Sitemap: {urlService.Build("/sitemap.xml")}\n" : string.Empty), "text/plain; charset=utf-8");
}

internal static class SitemapQueries
{
    public static Task<List<string>> CategoriesForSitemap(this ThuongMaiDienTuDbContext context, CancellationToken cancellationToken) =>
        context.ProductCategories.AsNoTracking().Where(item => item.Status == "ACTIVE").Select(item => "/danh-muc/" + item.Slug).ToListAsync(cancellationToken);
}
