namespace ThuongMaiDienTu.Services;

public interface ICarrierOwnershipService
{
    long? CurrentCarrierUserId { get; }
}

public sealed class CarrierOwnershipService(ICurrentUserService currentUser)
    : ICarrierOwnershipService
{
    public long? CurrentCarrierUserId =>
        string.Equals(currentUser.Role, "CARRIER", StringComparison.Ordinal)
            ? currentUser.UserId
            : null;
}
