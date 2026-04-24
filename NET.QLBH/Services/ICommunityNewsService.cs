using QLBH.ViewModels;

namespace QLBH.Services;

public interface ICommunityNewsService
{
    Task<IReadOnlyList<CommunityNewsItemViewModel>> GetLatestWoodNewsAsync(string? keyword, int limit = 12, CancellationToken cancellationToken = default);
}
