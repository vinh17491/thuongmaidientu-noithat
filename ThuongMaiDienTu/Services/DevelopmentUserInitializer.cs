using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ThuongMaiDienTu.Data;

namespace ThuongMaiDienTu.Services;

public static partial class DevelopmentUserInitializer
{
    // Chỉ dùng cho tài khoản ADMIN/SELLER mẫu có password_hash dạng placeholder.
    public const string DemoPassword = "Demo@123";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ThuongMaiDienTuDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(DevelopmentUserInitializer));

        var users = await context.Users
            .Where(user => user.Role == "ADMIN" || user.Role == "SELLER")
            .ToListAsync();

        var updatedEmails = new List<string>();

        foreach (var user in users)
        {
            if (IsBcryptHash(user.PasswordHash))
            {
                continue;
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword);
            user.UpdatedAt = DateTime.Now;
            updatedEmails.Add(user.Email);
        }

        if (updatedEmails.Count > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation(
                "Đã khởi tạo BCrypt cho tài khoản quản trị mẫu: {Emails}.",
                string.Join(", ", updatedEmails));
        }

    }

    private static bool IsBcryptHash(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && BcryptHashPattern().IsMatch(value);
    }

    [GeneratedRegex(@"^\$2[aby]\$\d{2}\$[./A-Za-z0-9]{53}$")]
    private static partial Regex BcryptHashPattern();
}
