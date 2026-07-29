using System.Security.Claims;

namespace ThuongMaiDienTu.Services;

public interface ICurrentUserService
{
    long? UserId { get; }
    string? Role { get; }
    bool IsAdmin { get; }
}

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor)
    : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public long? UserId => long.TryParse(
        User?.FindFirstValue(ClaimTypes.NameIdentifier), out var value)
        ? value
        : null;

    public string? Role => User?.FindFirstValue(ClaimTypes.Role);

    public bool IsAdmin => string.Equals(Role, "ADMIN", StringComparison.Ordinal);
}
