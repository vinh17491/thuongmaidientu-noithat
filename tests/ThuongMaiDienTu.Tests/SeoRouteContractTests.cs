using Microsoft.AspNetCore.Mvc;
using ThuongMaiDienTu.Controllers;
using Xunit;

namespace ThuongMaiDienTu.Tests;

public sealed class SeoRouteContractTests
{
    [Fact]
    public void PublicControllersExposeCanonicalRoutes()
    {
        Assert.Contains(typeof(ProductsController).GetMethods(), method => method.GetCustomAttributes(typeof(HttpGetAttribute), false).Cast<HttpGetAttribute>().Any(attribute => attribute.Template == "/san-pham"));
        Assert.Contains(typeof(ProductsController).GetMethods(), method => method.GetCustomAttributes(typeof(HttpGetAttribute), false).Cast<HttpGetAttribute>().Any(attribute => attribute.Template == "/san-pham/{productSlug}"));
        Assert.Contains(typeof(CategoriesController).GetMethods(), method => method.GetCustomAttributes(typeof(HttpGetAttribute), false).Cast<HttpGetAttribute>().Any(attribute => attribute.Template == "{categorySlug}"));
        Assert.Contains(typeof(StoresController).GetMethods(), method => method.GetCustomAttributes(typeof(HttpGetAttribute), false).Cast<HttpGetAttribute>().Any(attribute => attribute.Template == "{storeSlug}"));
    }

    [Fact]
    public void SeoEndpointsExposeXmlAndRobotsRoutes()
    {
        Assert.Contains(typeof(SeoController).GetMethods(), method => method.Name == "Sitemap" && method.GetCustomAttributes(typeof(HttpGetAttribute), false).Cast<HttpGetAttribute>().Any(attribute => attribute.Template == "/sitemap.xml"));
        Assert.Contains(typeof(SeoController).GetMethods(), method => method.Name == "Robots" && method.GetCustomAttributes(typeof(HttpGetAttribute), false).Cast<HttpGetAttribute>().Any(attribute => attribute.Template == "/robots.txt"));
    }
}
