using MailKit.Net.Smtp;
using MimeKit;

namespace Dashboards_reports.CollectionTracker.Services;

public interface IEmailService
{
    Task SendHtmlEmailAsync(
        IReadOnlyList<string> recipients,
        string subject,
        string bodyHtml,
        CancellationToken cancellationToken = default);
}

public sealed class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendHtmlEmailAsync(
        IReadOnlyList<string> recipients,
        string subject,
        string bodyHtml,
        CancellationToken cancellationToken = default)
    {
        var smtp = _config.GetSection("Smtp");
        var host = smtp["Host"] ?? "smtp.gmail.com";
        var port = int.Parse(smtp["Port"] ?? "587");
        var username = smtp["Username"] ?? "";
        var password = smtp["Password"] ?? "";
        var fromEmail = smtp["FromEmail"] ?? username;
        var fromName = smtp["FromName"] ?? "Collection Tracker";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));

        foreach (var recipient in recipients)
        {
            message.To.Add(MailboxAddress.Parse(recipient));
        }

        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = bodyHtml };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(username, password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        _logger.LogInformation("Insights report sent to {Count} recipients", recipients.Count);
    }
}
