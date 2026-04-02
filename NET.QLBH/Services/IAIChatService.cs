namespace QLBH.Services;

public interface IAIChatService
{
    Task<string> AskAsync(string message, CancellationToken cancellationToken = default);
}
