using QLBH.ViewModels;

namespace QLBH.Services;

public interface ICommunityFeedService
{
    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CommunityFeedItemViewModel>> GetReviewPostsAsync(int? currentUserId, string? keyword, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CreateReviewPostAsync(int userId, int? productId, int rating, string title, string content, IReadOnlyList<string> imageUrls, CancellationToken cancellationToken = default);
    Task AddCommentAsync(int postId, int userId, string content, int? parentCommentId = null, CancellationToken cancellationToken = default);
    Task ToggleReactionAsync(int postId, int userId, string reactionType = "useful", CancellationToken cancellationToken = default);
    Task HidePostAsync(int postId, CancellationToken cancellationToken = default);
    Task HideCommentAsync(int commentId, CancellationToken cancellationToken = default);
}
