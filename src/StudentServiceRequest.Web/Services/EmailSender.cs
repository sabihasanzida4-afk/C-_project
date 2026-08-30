using Microsoft.AspNetCore.Identity.UI.Services;

namespace StudentServiceRequest.Web.Services;

public class EmailSender : IEmailSender
{
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(ILogger<EmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        _logger.LogInformation("=== EMAIL SENT ===");
        _logger.LogInformation("To: {Email}", email);
        _logger.LogInformation("Subject: {Subject}", subject);
        _logger.LogInformation("Body: {Body}", htmlMessage);
        _logger.LogInformation("==================");
        return Task.CompletedTask;
    }
}