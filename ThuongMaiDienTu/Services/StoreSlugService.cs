using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace ThuongMaiDienTu.Services;

public static partial class StoreSlugService
{
    public static string Normalize(string value)
    {
        var form = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var character in form)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        var slug = NonSlugCharacters().Replace(builder.ToString().Normalize(NormalizationForm.FormC), "-");
        return slug.Trim('-').ToLowerInvariant();
    }

    public static async Task<string> CreateUniqueAsync(
        ThuongMaiDienTu.Data.ThuongMaiDienTuDbContext context,
        string storeName,
        long? excludedStoreId = null,
        CancellationToken cancellationToken = default)
    {
        var baseSlug = Normalize(storeName);
        if (string.IsNullOrWhiteSpace(baseSlug)) baseSlug = "store";
        var candidate = baseSlug;
        var suffix = 2;
        while (await context.Stores.AnyAsync(store =>
            store.Slug == candidate && (!excludedStoreId.HasValue || store.StoreId != excludedStoreId.Value), cancellationToken))
        {
            candidate = $"{baseSlug}-{suffix++}";
        }
        return candidate;
    }

    [GeneratedRegex("[^a-zA-Z0-9]+")]
    private static partial Regex NonSlugCharacters();
}
