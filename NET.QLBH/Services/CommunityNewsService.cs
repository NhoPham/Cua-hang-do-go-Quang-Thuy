using System.Text.RegularExpressions;
using System.Xml.Linq;
using QLBH.ViewModels;

namespace QLBH.Services;

public class CommunityNewsService : ICommunityNewsService
{
    private readonly HttpClient _httpClient;
    private static readonly Dictionary<string, (DateTime ExpiredAt, IReadOnlyList<CommunityNewsItemViewModel> Items)> Cache = new();
    private static readonly SemaphoreSlim CacheLock = new(1, 1);

    public CommunityNewsService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("QLBH-Wood-Community/1.0");
    }

    public async Task<IReadOnlyList<CommunityNewsItemViewModel>> GetLatestWoodNewsAsync(string? keyword, int limit = 12, CancellationToken cancellationToken = default)
    {
        var normalizedKeyword = string.IsNullOrWhiteSpace(keyword)
            ? "đồ gỗ OR nội thất gỗ OR gỗ tự nhiên OR thị trường gỗ"
            : keyword.Trim();

        var cacheKey = normalizedKeyword.ToLowerInvariant();

        await CacheLock.WaitAsync(cancellationToken);
        try
        {
            if (Cache.TryGetValue(cacheKey, out var cached) && cached.ExpiredAt > DateTime.UtcNow)
            {
                return cached.Items.Take(limit).ToList();
            }
        }
        finally
        {
            CacheLock.Release();
        }

        try
        {
            var query = Uri.EscapeDataString($"{normalizedKeyword} when:30d");
            var url = $"https://news.google.com/rss/search?q={query}&hl=vi&gl=VN&ceid=VN:vi";
            var xml = await _httpClient.GetStringAsync(url, cancellationToken);
            var document = XDocument.Parse(xml);

            var items = document.Descendants("item")
                .Select(item => new CommunityNewsItemViewModel
                {
                    Title = Clean(item.Element("title")?.Value),
                    Url = item.Element("link")?.Value ?? string.Empty,
                    Summary = Clean(item.Element("description")?.Value),
                    SourceName = Clean(item.Element("source")?.Value) is { Length: > 0 } source ? source : "Google News",
                    PublishedAt = DateTime.TryParse(item.Element("pubDate")?.Value, out var publishedAt) ? publishedAt.ToUniversalTime() : DateTime.UtcNow
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.Title) && !string.IsNullOrWhiteSpace(item.Url))
                .OrderByDescending(item => item.PublishedAt)
                .Take(Math.Max(limit, 20))
                .ToList();

            await CacheLock.WaitAsync(cancellationToken);
            try
            {
                Cache[cacheKey] = (DateTime.UtcNow.AddMinutes(20), items);
            }
            finally
            {
                CacheLock.Release();
            }

            return items.Take(limit).ToList();
        }
        catch
        {
            return Array.Empty<CommunityNewsItemViewModel>();
        }
    }

    private static string Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var noHtml = Regex.Replace(value, "<.*?>", " ");
        noHtml = System.Net.WebUtility.HtmlDecode(noHtml);
        noHtml = Regex.Replace(noHtml, "\\s+", " ").Trim();
        return noHtml.Length > 500 ? noHtml[..500] + "..." : noHtml;
    }
}
