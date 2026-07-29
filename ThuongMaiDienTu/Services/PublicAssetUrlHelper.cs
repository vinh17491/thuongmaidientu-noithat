namespace ThuongMaiDienTu.Services;

public static class PublicAssetUrlHelper
{
    public static bool IsSafeImageUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value.StartsWith("/", StringComparison.Ordinal) &&
            !value.StartsWith("//", StringComparison.Ordinal) &&
            !value.Contains("..", StringComparison.Ordinal))
        {
            return true;
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttps ||
                uri.Scheme == Uri.UriSchemeHttp);
    }
}
