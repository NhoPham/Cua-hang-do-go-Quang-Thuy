namespace QLBH.Services;

public interface IChatAssistantService
{
    Task<string> AskAsync(string message, CancellationToken cancellationToken = default);
}
