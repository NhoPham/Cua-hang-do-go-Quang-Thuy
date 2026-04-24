using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLBH.Models;
using QLBH.Services;
using QLBH.Utils;
using QLBH.ViewModels;

namespace QLBH.Controllers;

public class CommunityController : Controller
{
    private readonly QlbhContext _context;
    private readonly ICommunityNewsService _newsService;
    private readonly ICommunityFeedService _feedService;
    private readonly IWebHostEnvironment _environment;

    public CommunityController(
        QlbhContext context,
        ICommunityNewsService newsService,
        ICommunityFeedService feedService,
        IWebHostEnvironment environment)
    {
        _context = context;
        _newsService = newsService;
        _feedService = feedService;
        _environment = environment;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index(string? q, string filter = "all", int page = 1, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        filter = string.IsNullOrWhiteSpace(filter) ? "all" : filter.Trim().ToLowerInvariant();

        var currentUserId = User.GetUserId();

var products = new List<ProductSelectItemViewModel>();

if (currentUserId.HasValue)
{
    var purchasedProductIds = await _context.Orders
        .AsNoTracking()
        .Where(o => o.UserId == currentUserId.Value && o.OrderStatus == OrderStatuses.Completed)
        .SelectMany(o => o.Items)
        .Select(i => i.ProductId)
        .Distinct()
        .ToListAsync(cancellationToken);

    products = await _context.Products
        .AsNoTracking()
        .Where(p => purchasedProductIds.Contains(p.Id))
        .OrderBy(p => p.Name)
        .Select(p => new ProductSelectItemViewModel
        {
            Id = p.Id,
            Name = p.Name
        })
        .ToListAsync(cancellationToken);
}

        var items = new List<CommunityFeedItemViewModel>();

        if (filter is "all" or "reviews")
        {
            var reviews = await _feedService.GetReviewPostsAsync(currentUserId, q, page, 20, cancellationToken);
            items.AddRange(reviews);
        }

        if (filter is "all" or "news")
        {
            var newsItems = await _newsService.GetLatestWoodNewsAsync(q, 12, cancellationToken);
            items.AddRange(newsItems.Select(item => new CommunityFeedItemViewModel
            {
                ItemType = "news",
                Badge = "Tin tức mới",
                Title = item.Title,
                Content = item.Summary,
                CreatedAt = item.PublishedAt,
                AuthorName = item.SourceName,
                SourceName = item.SourceName,
                SourceUrl = item.Url
            }));
        }

        var model = new CommunityFeedPageViewModel
        {
            Search = q,
            Filter = filter,
            CurrentPage = page,
            IsAuthenticated = User.Identity?.IsAuthenticated == true,
            IsAdmin = User.IsInRole("admin"),
            Products = products,
            Items = items.OrderByDescending(x => x.CreatedAt).ToList()
        };

        ViewData["Title"] = "Cộng đồng đồ gỗ";
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostReview(int? productId, int rating, string title, string content, List<IFormFile>? images, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Community") });
        }

        title = title?.Trim() ?? string.Empty;
        content = content?.Trim() ?? string.Empty;

        if (rating < 1 || rating > 5)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn số sao từ 1 đến 5.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập tiêu đề và nội dung đánh giá.";
            return RedirectToAction(nameof(Index));
        }

        var imageUrls = await SaveImagesAsync(images, cancellationToken);
        if (!productId.HasValue)
{
    TempData["ErrorMessage"] = "Bạn chỉ có thể đánh giá sản phẩm đã mua.";
    return RedirectToAction(nameof(Index), new { filter = "reviews" });
}

var hasPurchasedProduct = await _context.Orders
    .AsNoTracking()
    .AnyAsync(o =>
        o.UserId == userId.Value &&
        o.OrderStatus == OrderStatuses.Completed &&
        o.Items.Any(i => i.ProductId == productId.Value),
        cancellationToken);

if (!hasPurchasedProduct)
{
    TempData["ErrorMessage"] = "Bạn chưa mua sản phẩm này hoặc đơn hàng chưa hoàn thành nên chưa thể đánh giá.";
    return RedirectToAction(nameof(Index), new { filter = "reviews" });
}
        await _feedService.CreateReviewPostAsync(userId.Value, productId, rating, title, content, imageUrls, cancellationToken);

        TempData["SuccessMessage"] = "Đăng bài đánh giá thành công.";
        return RedirectToAction(nameof(Index), new { filter = "reviews" });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Comment(int postId, string content, int? parentCommentId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Community") });
        }

        if (!string.IsNullOrWhiteSpace(content))
        {
            await _feedService.AddCommentAsync(postId, userId.Value, content, parentCommentId, cancellationToken);
        }

        return RedirectToAction(nameof(Index), new { filter = "reviews" });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> React(int postId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId.HasValue)
        {
            await _feedService.ToggleReactionAsync(postId, userId.Value, "useful", cancellationToken);
        }

        return RedirectToAction(nameof(Index), new { filter = "reviews" });
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HidePost(int postId, CancellationToken cancellationToken)
    {
        await _feedService.HidePostAsync(postId, cancellationToken);
        TempData["SuccessMessage"] = "Đã ẩn bài viết.";
        return RedirectToAction(nameof(Index), new { filter = "reviews" });
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HideComment(int commentId, CancellationToken cancellationToken)
    {
        await _feedService.HideCommentAsync(commentId, cancellationToken);
        TempData["SuccessMessage"] = "Đã ẩn bình luận.";
        return RedirectToAction(nameof(Index), new { filter = "reviews" });
    }

    private async Task<List<string>> SaveImagesAsync(List<IFormFile>? files, CancellationToken cancellationToken)
    {
        var result = new List<string>();
        if (files == null || files.Count == 0)
        {
            return result;
        }

        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
        var rootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadFolder = Path.Combine(rootPath, "images", "community");
        Directory.CreateDirectory(uploadFolder);

        foreach (var file in files.Where(x => x.Length > 0).Take(5))
        {
            var extension = Path.GetExtension(file.FileName);
            if (!allowedExtensions.Contains(extension))
            {
                continue;
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                continue;
            }

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(uploadFolder, fileName);
            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream, cancellationToken);
            result.Add($"/images/community/{fileName}");
        }

        return result;
    }
}
