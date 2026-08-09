namespace ThuongMaiDienTu.Services;

public sealed class SeoUrlService(IConfiguration configuration)
{
    private readonly Uri? baseUrl = Parse(configuration["Seo:BaseUrl"]);
    public bool HasValidBaseUrl => baseUrl is not null;
    public string Build(string path) => baseUrl is null ? "/" + path.TrimStart('/') : new Uri(baseUrl, path.TrimStart('/')).ToString();
    private static Uri? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsControl) || !Uri.TryCreate(value.TrimEnd('/'), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) || uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0) return null;
        return uri;
    }
}
