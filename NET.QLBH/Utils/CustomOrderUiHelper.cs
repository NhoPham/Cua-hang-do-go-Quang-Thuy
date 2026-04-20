namespace QLBH.Utils;

public static class CustomOrderStatuses
{
    public const string New = "new";
    public const string Consulting = "consulting";
    public const string Quoted = "quoted";
    public const string AwaitingConfirmation = "awaiting_confirmation";
    public const string Approved = "approved";
    public const string InProduction = "in_production";
    public const string ReadyToShip = "ready_to_ship";
    public const string Shipping = "shipping";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";

    public static readonly IReadOnlyList<string> All = new[]
    {
        New,
        Consulting,
        Quoted,
        AwaitingConfirmation,
        Approved,
        InProduction,
        ReadyToShip,
        Shipping,
        Completed,
        Cancelled
    };
}

public static class CustomOrderUiHelper
{
    public static string NormalizeStatus(string? status)
    {
        var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();
        return CustomOrderStatuses.All.Contains(normalized)
            ? normalized
            : CustomOrderStatuses.New;
    }

    public static string StatusLabel(string? status)
    {
        return NormalizeStatus(status) switch
        {
            CustomOrderStatuses.New => "Mới tiếp nhận",
            CustomOrderStatuses.Consulting => "Đang tư vấn",
            CustomOrderStatuses.Quoted => "Đã báo giá",
            CustomOrderStatuses.AwaitingConfirmation => "Chờ khách xác nhận",
            CustomOrderStatuses.Approved => "Đã duyệt",
            CustomOrderStatuses.InProduction => "Đang sản xuất",
            CustomOrderStatuses.ReadyToShip => "Sẵn sàng giao",
            CustomOrderStatuses.Shipping => "Đang giao",
            CustomOrderStatuses.Completed => "Hoàn thành",
            CustomOrderStatuses.Cancelled => "Đã hủy",
            _ => "Đang xử lý"
        };
    }

    public static string StatusBadgeClass(string? status)
    {
        return NormalizeStatus(status) switch
        {
            CustomOrderStatuses.New => "bg-secondary",
            CustomOrderStatuses.Consulting => "bg-info text-dark",
            CustomOrderStatuses.Quoted => "bg-primary",
            CustomOrderStatuses.AwaitingConfirmation => "bg-warning text-dark",
            CustomOrderStatuses.Approved => "bg-success",
            CustomOrderStatuses.InProduction => "bg-dark",
            CustomOrderStatuses.ReadyToShip => "bg-success",
            CustomOrderStatuses.Shipping => "bg-primary",
            CustomOrderStatuses.Completed => "bg-success",
            CustomOrderStatuses.Cancelled => "bg-danger",
            _ => "bg-secondary"
        };
    }

    public static IReadOnlyList<string> ParseReferenceImageUrls(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        return raw
            .Split(new[] { '\n', '\r', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
