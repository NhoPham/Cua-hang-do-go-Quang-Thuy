using System.Data;
using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QLBH.Models;
using QLBH.ViewModels;

namespace QLBH.Services;

public class CommunityFeedService : ICommunityFeedService
{
    private readonly QlbhContext _context;

    public CommunityFeedService(QlbhContext context)
    {
        _context = context;
    }

    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        var sql = @"
IF OBJECT_ID(N'dbo.CommunityPost', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CommunityPost
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CommunityPost PRIMARY KEY,
        user_id INT NOT NULL,
        product_id INT NULL,
        post_type NVARCHAR(50) NOT NULL CONSTRAINT DF_CommunityPost_post_type DEFAULT N'ProductReview',
        title NVARCHAR(255) NOT NULL,
        content NVARCHAR(2000) NOT NULL,
        rating INT NULL,
        image_urls NVARCHAR(MAX) NULL,
        status NVARCHAR(50) NOT NULL CONSTRAINT DF_CommunityPost_status DEFAULT N'visible',
        created_at DATETIME2 NOT NULL CONSTRAINT DF_CommunityPost_created_at DEFAULT SYSUTCDATETIME(),
        updated_at DATETIME2 NULL,
        CONSTRAINT FK_CommunityPost_User FOREIGN KEY(user_id) REFERENCES dbo.[User](id),
        CONSTRAINT FK_CommunityPost_Product FOREIGN KEY(product_id) REFERENCES dbo.Product(id) ON DELETE SET NULL
    );
END;

IF OBJECT_ID(N'dbo.CommunityPostComment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CommunityPostComment
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CommunityPostComment PRIMARY KEY,
        post_id INT NOT NULL,
        user_id INT NOT NULL,
        parent_comment_id INT NULL,
        content NVARCHAR(1000) NOT NULL,
        status NVARCHAR(50) NOT NULL CONSTRAINT DF_CommunityPostComment_status DEFAULT N'visible',
        created_at DATETIME2 NOT NULL CONSTRAINT DF_CommunityPostComment_created_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_CommunityPostComment_Post FOREIGN KEY(post_id) REFERENCES dbo.CommunityPost(id) ON DELETE CASCADE,
        CONSTRAINT FK_CommunityPostComment_User FOREIGN KEY(user_id) REFERENCES dbo.[User](id),
        CONSTRAINT FK_CommunityPostComment_Parent FOREIGN KEY(parent_comment_id) REFERENCES dbo.CommunityPostComment(id)
    );
END;

IF OBJECT_ID(N'dbo.CommunityPostReaction', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CommunityPostReaction
    (
        id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CommunityPostReaction PRIMARY KEY,
        post_id INT NOT NULL,
        user_id INT NOT NULL,
        reaction_type NVARCHAR(50) NOT NULL CONSTRAINT DF_CommunityPostReaction_reaction_type DEFAULT N'useful',
        created_at DATETIME2 NOT NULL CONSTRAINT DF_CommunityPostReaction_created_at DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_CommunityPostReaction_Post FOREIGN KEY(post_id) REFERENCES dbo.CommunityPost(id) ON DELETE CASCADE,
        CONSTRAINT FK_CommunityPostReaction_User FOREIGN KEY(user_id) REFERENCES dbo.[User](id)
    );

    CREATE UNIQUE INDEX UX_CommunityPostReaction_Post_User_Type
    ON dbo.CommunityPostReaction(post_id, user_id, reaction_type);
END;
";

        await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    public async Task<IReadOnlyList<CommunityFeedItemViewModel>> GetReviewPostsAsync(int? currentUserId, string? keyword, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await EnsureCreatedAsync(cancellationToken);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);
        var skip = (page - 1) * pageSize;
        var search = string.IsNullOrWhiteSpace(keyword) ? null : $"%{keyword.Trim()}%";

        var posts = new List<CommunityFeedItemViewModel>();
        var connection = _context.Database.GetDbConnection();
        await OpenIfNeededAsync(connection, cancellationToken);

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
SELECT
    p.id,
    p.product_id,
    p.title,
    p.content,
    p.rating,
    p.image_urls,
    p.created_at,
    u.username AS author_name,
    pr.name AS product_name,
    (SELECT COUNT(1) FROM dbo.CommunityPostReaction r WHERE r.post_id = p.id) AS reaction_count,
    (SELECT COUNT(1) FROM dbo.CommunityPostComment c WHERE c.post_id = p.id AND c.status = N'visible') AS comment_count,
    CASE WHEN @currentUserId IS NULL THEN 0 ELSE
        CASE WHEN EXISTS (SELECT 1 FROM dbo.CommunityPostReaction r WHERE r.post_id = p.id AND r.user_id = @currentUserId) THEN 1 ELSE 0 END
    END AS reacted_by_current_user
FROM dbo.CommunityPost p
INNER JOIN dbo.[User] u ON u.id = p.user_id
LEFT JOIN dbo.Product pr ON pr.id = p.product_id
WHERE p.status = N'visible'
  AND p.post_type = N'ProductReview'
  AND (@search IS NULL OR p.title LIKE @search OR p.content LIKE @search OR pr.name LIKE @search)
ORDER BY p.created_at DESC
OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;";
            AddParameter(command, "@currentUserId", currentUserId);
            AddParameter(command, "@search", search);
            AddParameter(command, "@skip", skip);
            AddParameter(command, "@take", pageSize);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                posts.Add(new CommunityFeedItemViewModel
                {
                    ItemType = "review",
                    Badge = "Đánh giá sản phẩm",
                    PostId = reader.GetInt32(reader.GetOrdinal("id")),
                    ProductId = reader.IsDBNull(reader.GetOrdinal("product_id")) ? null : reader.GetInt32(reader.GetOrdinal("product_id")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Content = reader.GetString(reader.GetOrdinal("content")),
                    Rating = reader.IsDBNull(reader.GetOrdinal("rating")) ? null : reader.GetInt32(reader.GetOrdinal("rating")),
                    ImageUrls = ParseImages(reader.IsDBNull(reader.GetOrdinal("image_urls")) ? null : reader.GetString(reader.GetOrdinal("image_urls"))),
                    CreatedAt = DateTime.SpecifyKind(reader.GetDateTime(reader.GetOrdinal("created_at")), DateTimeKind.Utc),
                    AuthorName = reader.GetString(reader.GetOrdinal("author_name")),
                    ProductName = reader.IsDBNull(reader.GetOrdinal("product_name")) ? null : reader.GetString(reader.GetOrdinal("product_name")),
                    ReactionCount = reader.GetInt32(reader.GetOrdinal("reaction_count")),
                    CommentCount = reader.GetInt32(reader.GetOrdinal("comment_count")),
                    ReactedByCurrentUser = reader.GetInt32(reader.GetOrdinal("reacted_by_current_user")) == 1
                });
            }
        }

        if (posts.Count == 0)
        {
            return posts;
        }

        var ids = posts.Where(p => p.PostId.HasValue).Select(p => p.PostId!.Value).ToList();
        var comments = await GetCommentsAsync(ids, connection, cancellationToken);
        foreach (var post in posts)
        {
            if (post.PostId.HasValue)
            {
                post.Comments = comments.Where(c => c.PostId == post.PostId.Value).Take(8).ToList();
            }
        }

        return posts;
    }

    public async Task<int> CreateReviewPostAsync(int userId, int? productId, int rating, string title, string content, IReadOnlyList<string> imageUrls, CancellationToken cancellationToken = default)
    {
        await EnsureCreatedAsync(cancellationToken);

        rating = Math.Clamp(rating, 1, 5);
        title = title.Trim();
        content = content.Trim();
        var imagesJson = JsonSerializer.Serialize(imageUrls ?? Array.Empty<string>());

        var connection = _context.Database.GetDbConnection();
        await OpenIfNeededAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO dbo.CommunityPost(user_id, product_id, post_type, title, content, rating, image_urls, status, created_at)
OUTPUT INSERTED.id
VALUES(@userId, @productId, N'ProductReview', @title, @content, @rating, @imageUrls, N'visible', SYSUTCDATETIME());";
        AddParameter(command, "@userId", userId);
        AddParameter(command, "@productId", productId);
        AddParameter(command, "@title", title);
        AddParameter(command, "@content", content);
        AddParameter(command, "@rating", rating);
        AddParameter(command, "@imageUrls", imagesJson);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task AddCommentAsync(int postId, int userId, string content, int? parentCommentId = null, CancellationToken cancellationToken = default)
    {
        await EnsureCreatedAsync(cancellationToken);

        content = content.Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        var connection = _context.Database.GetDbConnection();
        await OpenIfNeededAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO dbo.CommunityPostComment(post_id, user_id, parent_comment_id, content, status, created_at)
VALUES(@postId, @userId, @parentCommentId, @content, N'visible', SYSUTCDATETIME());";
        AddParameter(command, "@postId", postId);
        AddParameter(command, "@userId", userId);
        AddParameter(command, "@parentCommentId", parentCommentId);
        AddParameter(command, "@content", content);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ToggleReactionAsync(int postId, int userId, string reactionType = "useful", CancellationToken cancellationToken = default)
    {
        await EnsureCreatedAsync(cancellationToken);

        reactionType = string.IsNullOrWhiteSpace(reactionType) ? "useful" : reactionType.Trim();
        var connection = _context.Database.GetDbConnection();
        await OpenIfNeededAsync(connection, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
IF EXISTS (SELECT 1 FROM dbo.CommunityPostReaction WHERE post_id = @postId AND user_id = @userId AND reaction_type = @reactionType)
BEGIN
    DELETE FROM dbo.CommunityPostReaction WHERE post_id = @postId AND user_id = @userId AND reaction_type = @reactionType;
END
ELSE
BEGIN
    INSERT INTO dbo.CommunityPostReaction(post_id, user_id, reaction_type, created_at)
    VALUES(@postId, @userId, @reactionType, SYSUTCDATETIME());
END;";
        AddParameter(command, "@postId", postId);
        AddParameter(command, "@userId", userId);
        AddParameter(command, "@reactionType", reactionType);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task HidePostAsync(int postId, CancellationToken cancellationToken = default)
    {
        await EnsureCreatedAsync(cancellationToken);
        await _context.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.CommunityPost SET status = N'hidden', updated_at = SYSUTCDATETIME() WHERE id = {postId}", cancellationToken);
    }

    public async Task HideCommentAsync(int commentId, CancellationToken cancellationToken = default)
    {
        await EnsureCreatedAsync(cancellationToken);
        await _context.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.CommunityPostComment SET status = N'hidden' WHERE id = {commentId}", cancellationToken);
    }

    private static async Task<List<CommunityCommentViewModel>> GetCommentsAsync(IReadOnlyList<int> postIds, DbConnection connection, CancellationToken cancellationToken)
    {
        var comments = new List<CommunityCommentViewModel>();
        if (postIds.Count == 0)
        {
            return comments;
        }

        await using var command = connection.CreateCommand();
        var parameterNames = new List<string>();
        for (var i = 0; i < postIds.Count; i++)
        {
            var name = $"@id{i}";
            parameterNames.Add(name);
            AddParameter(command, name, postIds[i]);
        }

        command.CommandText = $@"
SELECT c.id, c.post_id, c.parent_comment_id, c.content, c.created_at, u.username AS author_name
FROM dbo.CommunityPostComment c
INNER JOIN dbo.[User] u ON u.id = c.user_id
WHERE c.status = N'visible' AND c.post_id IN ({string.Join(",", parameterNames)})
ORDER BY c.created_at ASC;";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            comments.Add(new CommunityCommentViewModel
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                PostId = reader.GetInt32(reader.GetOrdinal("post_id")),
                ParentCommentId = reader.IsDBNull(reader.GetOrdinal("parent_comment_id")) ? null : reader.GetInt32(reader.GetOrdinal("parent_comment_id")),
                Content = reader.GetString(reader.GetOrdinal("content")),
                CreatedAt = DateTime.SpecifyKind(reader.GetDateTime(reader.GetOrdinal("created_at")), DateTimeKind.Utc),
                AuthorName = reader.GetString(reader.GetOrdinal("author_name"))
            });
        }

        return comments;
    }

    private static async Task OpenIfNeededAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static List<string> ParseImages(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
