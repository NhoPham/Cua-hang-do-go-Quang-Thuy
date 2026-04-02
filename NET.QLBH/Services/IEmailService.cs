namespace QLBH.Services;

public interface IEmailService
{
    Task<bool> SendAsync(string toEmail, string subject, string htmlBody);
}
