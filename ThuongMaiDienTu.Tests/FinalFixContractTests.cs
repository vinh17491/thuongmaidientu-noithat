using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using ThuongMaiDienTu.Controllers;
using ThuongMaiDienTu.Services;
using Xunit;

namespace ThuongMaiDienTu.Tests;

public sealed class FinalFixContractTests
{
    private static readonly string Root = FindRoot();

    [Theory]
    [InlineData("tìm bàn làm việc", "bàn làm việc")]
    [InlineData("tim ban lam viec", "ban lam viec")]
    public void ProductIntentPrefixIsRemoved(string message, string expected) =>
        Assert.Equal(expected, LocalChatAssistantService.ExtractSearchTerm(message, "product_search"));

    [Fact]
    public void VietnameseTextCanBeNormalizedWithoutDiacritics() =>
        Assert.Equal("tim ban lam viec", LocalChatAssistantService.NormalizeText("Tìm bàn làm việc"));

    [Fact]
    public void ChatEndpointUsesNamedAspNetCoreRateLimitPolicy()
    {
        var method = typeof(ChatAssistantController).GetMethod(nameof(ChatAssistantController.Ask));
        var attribute = Assert.Single(method!.GetCustomAttributes<EnableRateLimitingAttribute>());
        Assert.Equal("ChatAssistant", attribute.PolicyName);
    }

    [Fact]
    public void LayoutHasOneFormDataSubmitHandlerAndAntiforgery()
    {
        var layout = Read("ThuongMaiDienTu", "Views", "Shared", "_Layout.cshtml");
        Assert.Single(Regex.Matches(layout, "form\\.addEventListener\\('submit'").Cast<Match>());
        Assert.Contains("new FormData(form)", layout);
        Assert.Contains("@Html.AntiForgeryToken()", layout);
        Assert.DoesNotContain("application/json", layout);
        Assert.Contains("textContent", layout);
    }

    [Fact]
    public void CatalogUsesSlugLinkAndOneConditionalBreadcrumb()
    {
        var view = Read("ThuongMaiDienTu", "Views", "Products", "Index.cshtml");
        Assert.Contains("asp-action=\"CanonicalDetails\"", view);
        Assert.Contains("asp-route-productSlug=\"@item.Slug\"", view);
        Assert.DoesNotContain("asp-route-id=\"@item.ProductId\"", view);
        Assert.Single(Regex.Matches(view, "BreadcrumbList").Cast<Match>());
    }

    [Fact]
    public void OnlyDefaultConnectionIsConfigured()
    {
        using var document = JsonDocument.Parse(Read("ThuongMaiDienTu", "appsettings.json"));
        var connections = document.RootElement.GetProperty("ConnectionStrings");
        Assert.Single(connections.EnumerateObject());
        Assert.Contains("Database=thuongmaidientu", connections.GetProperty("DefaultConnection").GetString());
    }

    [Fact]
    public void SqlSeedRetiresBadmintonAndMaintainsTenHouseholdProducts()
    {
        var sql = Read("thuongmaidientu.sql");
        Assert.Contains("SET status='HIDDEN'", sql);
        Assert.Contains("SET status='SUSPENDED'", sql);
        Assert.Contains("SET status='INACTIVE'", sql);
        Assert.Contains("NOT BETWEEN 8 AND 12", sql);
        Assert.Equal(10, Regex.Matches(sql, @"VALUES\(@(?:RiceCooker|Blender|Cooktop|Kettle|Vacuum|Desk|Chair|Lamp|Shelf|Pan)Id,'/images/products/").Count);
    }

    [Fact]
    public void EveryHouseholdSeedImageExists()
    {
        var names = new[] { "noi-com-dien-sharp-18l", "may-xay-sinh-to-philips", "bep-dien-tu-sunhouse",
            "am-sieu-toc-locklock-17l", "may-hut-bui-cam-tay-deerma", "ban-lam-viec-go-soi",
            "ghe-an-boc-ni", "den-ban-led-chong-can", "ke-sach-go-ba-tang", "chao-chong-dinh-28cm" };
        foreach (var name in names)
            Assert.True(File.Exists(Path.Combine(Root, "ThuongMaiDienTu", "wwwroot", "images", "products", name + ".svg")), name);
    }

    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine([Root, .. parts]));

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "thuongmaidientu.sql")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}

public sealed class ChatRateLimitHttpTest
{
    [Fact]
    public async Task SixteenthChatRequestInOneMinuteReturns429()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseEnvironment("Testing").UseSetting("ConnectionStrings:DefaultConnection",
                "Server=localhost;Database=thuongmaidientu;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True"));
        using var client = factory.CreateClient();
        var login = await client.GetStringAsync("/Account/Login");
        var token = Regex.Match(login, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);
        for (var index = 0; index < 15; index++)
        {
            using var form = Form(token);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/tro-ly/hoi", form)).StatusCode);
        }
        using var rejectedForm = Form(token);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsync("/tro-ly/hoi", rejectedForm)).StatusCode);
    }

    private static MultipartFormDataContent Form(string token) => new()
    {
        { new StringContent("xin chào"), "Message" },
        { new StringContent(token), "__RequestVerificationToken" }
    };

    [Fact]
    public async Task CategoryHasOwnOgUrlAndParseableSingleBreadcrumb()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseEnvironment("Testing").UseSetting("ConnectionStrings:DefaultConnection",
                "Server=localhost;Database=thuongmaidientu;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True"));
        using var client = factory.CreateClient();
        var html = await client.GetStringAsync("/danh-muc/nha-bep");
        Assert.Contains("property=\"og:url\" content=\"/danh-muc/nha-bep\"", html);
        var scripts = Regex.Matches(html, "<script type=\"application/ld\\+json\">(.*?)</script>", RegexOptions.Singleline)
            .Select(match => match.Groups[1].Value)
            .Where(json => json.Contains("BreadcrumbList", StringComparison.Ordinal))
            .ToList();
        var breadcrumb = Assert.Single(scripts);
        using var document = JsonDocument.Parse(breadcrumb);
        Assert.Equal("BreadcrumbList", document.RootElement.GetProperty("@type").GetString());
    }
}
