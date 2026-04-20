namespace QLBH.Settings;

public class ChatAssistantSettings
{
    public string Endpoint { get; set; } = "http://localhost:11434/api/generate";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "llama3.2";
    public string SystemPrompt { get; set; } =
        "Bạn là trợ lý tư vấn bán hàng cho cửa hàng Đồ Gỗ Quảng Thủy. " +
        "Luôn trả lời bằng tiếng Việt, lịch sự, ngắn gọn, thực tế và dễ hiểu. " +
        "Ưu tiên tư vấn chọn nội thất gỗ, kích thước, công năng, bảo quản, ngân sách, " +
        "và hướng dẫn khách tạo yêu cầu đặt hàng theo yêu cầu nếu chưa có mẫu phù hợp. " +
        "Không bịa thông tin về tồn kho, giá hoặc chính sách khi không chắc chắn. " +
        "Nếu thiếu dữ liệu, hãy nói rõ cần nhân viên xác nhận thêm.";
    public int MaxCatalogItems { get; set; } = 12;
    public int MaxResponseTokens { get; set; } = 500;
}
