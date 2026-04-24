namespace QLBH.ViewModels;

public class CommunityFeedPageViewModel
{
    public string? Search { get; set; }
    public string Filter { get; set; } = "all";
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool IsAuthenticated { get; set; }
    public bool IsAdmin { get; set; }
    public List<ProductSelectItemViewModel> Products { get; set; } = new();
    public List<CommunityFeedItemViewModel> Items { get; set; } = new();
}

public class ProductSelectItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CommunityFeedItemViewModel
{
    public string ItemType { get; set; } = "review";
    public string Badge { get; set; } = "Đánh giá";
    public int? PostId { get; set; }
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int? Rating { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<string> ImageUrls { get; set; } = new();
    public int ReactionCount { get; set; }
    public int CommentCount { get; set; }
    public bool ReactedByCurrentUser { get; set; }
    public string? SourceName { get; set; }
    public string? SourceUrl { get; set; }
    public List<CommunityCommentViewModel> Comments { get; set; } = new();
}

public class CommunityCommentViewModel
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public int? ParentCommentId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CommunityNewsItemViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string SourceName { get; set; } = "Google News";
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
}
