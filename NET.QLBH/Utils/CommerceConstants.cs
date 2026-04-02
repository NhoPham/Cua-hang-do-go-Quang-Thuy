namespace QLBH.Utils;

public static class PaymentMethods
{
    public const string CashOnDelivery = "COD";
    public const string BankTransfer = "BANK_TRANSFER";

    public static readonly IReadOnlyList<string> All = new[]
    {
        CashOnDelivery,
        BankTransfer
    };
}

public static class PaymentStatuses
{
    public const string Pending = "PENDING";
    public const string Paid = "PAID";
    public const string Refunded = "REFUNDED";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Pending,
        Paid,
        Refunded
    };
}

public static class OrderStatuses
{
    public const string Pending = "PENDING";
    public const string Confirmed = "CONFIRMED";
    public const string Shipping = "SHIPPING";
    public const string Completed = "COMPLETED";
    public const string Cancelled = "CANCELLED";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Pending,
        Confirmed,
        Shipping,
        Completed,
        Cancelled
    };
}

public static class OrderUiHelper
{
    public static string OrderStatusLabel(string status) => status switch
    {
        OrderStatuses.Pending => "Chờ xác nhận",
        OrderStatuses.Confirmed => "Đã xác nhận",
        OrderStatuses.Shipping => "Đang giao",
        OrderStatuses.Completed => "Hoàn thành",
        OrderStatuses.Cancelled => "Đã hủy",
        _ => status
    };

    public static string OrderStatusBadgeClass(string status) => status switch
    {
        OrderStatuses.Pending => "bg-warning text-dark",
        OrderStatuses.Confirmed => "bg-info text-dark",
        OrderStatuses.Shipping => "bg-primary",
        OrderStatuses.Completed => "bg-success",
        OrderStatuses.Cancelled => "bg-danger",
        _ => "bg-secondary"
    };

    public static string PaymentMethodLabel(string method) => method switch
    {
        PaymentMethods.CashOnDelivery => "Thanh toán khi nhận hàng",
        PaymentMethods.BankTransfer => "Chuyển khoản",
        _ => method
    };

    public static string PaymentStatusLabel(string status) => status switch
    {
        PaymentStatuses.Pending => "Chưa thanh toán",
        PaymentStatuses.Paid => "Đã thanh toán",
        PaymentStatuses.Refunded => "Đã hoàn tiền",
        _ => status
    };

    public static string PaymentStatusBadgeClass(string status) => status switch
    {
        PaymentStatuses.Pending => "bg-warning text-dark",
        PaymentStatuses.Paid => "bg-success",
        PaymentStatuses.Refunded => "bg-secondary",
        _ => "bg-secondary"
    };
}
