namespace ThuongMaiDienTu.Services;

public static class StoreOrderWorkflow
{
    public const string Pending = "PENDING";
    public const string Confirmed = "CONFIRMED";
    public const string Processing = "PROCESSING";
    public const string Shipping = "SHIPPING";
    public const string Delivered = "DELIVERED";
    public const string Cancelled = "CANCELLED";

    public static bool CanTransition(
        string currentStatus,
        string nextStatus,
        bool carrierHasPickedUp = false) =>
        (currentStatus, nextStatus) switch
        {
            (Pending, Confirmed) => true,
            (Pending, Cancelled) => true,
            (Confirmed, Processing) => true,
            (Confirmed, Cancelled) => !carrierHasPickedUp,
            (Processing, Shipping) => true,
            (Shipping, Delivered) => true,
            _ => false
        };

    public static string AggregateParentStatus(IEnumerable<string> statuses)
    {
        var values = statuses.Distinct(StringComparer.Ordinal).ToArray();
        if (values.Length == 0)
        {
            return Pending;
        }

        if (values.All(status => status == Cancelled))
        {
            return Cancelled;
        }

        if (values.All(status => status is Delivered or Cancelled))
        {
            return Delivered;
        }

        if (values.Any(status => status == Shipping))
        {
            return Shipping;
        }

        if (values.Any(status => status == Processing))
        {
            return Processing;
        }

        if (values.Any(status => status == Confirmed))
        {
            return Confirmed;
        }

        return Pending;
    }

    public static decimal CalculateTotal(
        decimal subtotal,
        decimal discount,
        decimal shippingFee)
    {
        if (subtotal < 0 || discount < 0 || shippingFee < 0 || discount > subtotal)
        {
            throw new ArgumentOutOfRangeException(
                nameof(subtotal), "Giá trị tiền của đơn cửa hàng không hợp lệ.");
        }

        return subtotal - discount + shippingFee;
    }

    public static decimal CalculatePayableParentTotal(
        IEnumerable<(string Status, decimal TotalAmount)> storeOrders) =>
        storeOrders
            .Where(item => item.Status != Cancelled)
            .Sum(item => item.TotalAmount);
}

public static class ShipmentWorkflow
{
    public const string Created = "CREATED";
    public const string ReadyForPickup = "READY_FOR_PICKUP";
    public const string PickedUp = "PICKED_UP";
    public const string InTransit = "IN_TRANSIT";
    public const string Delivered = "DELIVERED";
    public const string Failed = "FAILED";
    public const string Cancelled = "CANCELLED";

    public static bool CanTransition(string currentStatus, string nextStatus) =>
        (currentStatus, nextStatus) switch
        {
            (Created, ReadyForPickup) => true,
            (Created, Cancelled) => true,
            (ReadyForPickup, PickedUp) => true,
            (ReadyForPickup, Cancelled) => true,
            (PickedUp, InTransit) => true,
            (PickedUp, Failed) => true,
            (InTransit, Delivered) => true,
            (InTransit, Failed) => true,
            (Failed, InTransit) => true,
            (Failed, Cancelled) => true,
            _ => false
        };
}
