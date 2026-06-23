using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TenderDocs.Application.Common.Interfaces;

namespace TenderDocs.Infrastructure.Services;

/// <summary>
/// SMTP email sender (optional feature — used for expiry reminders / notifications).
/// If Smtp:Host is not configured the sender becomes a no-op that just logs, so the
/// app runs fine without email in development.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
        => (_config, _logger) = (config, logger);

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var smtp = _config.GetSection("Smtp");
        var host = smtp["Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogInformation("SMTP not configured; skipping email to {To} ({Subject}).", to, subject);
            return;
        }

        var port = int.TryParse(smtp["Port"], out var p) ? p : 587;
        var enableSsl = !bool.TryParse(smtp["EnableSsl"], out var ssl) || ssl;
        var from = smtp["From"] ?? smtp["Username"] ?? "no-reply@tenderdocs.local";

        using var client = new SmtpClient(host, port) { EnableSsl = enableSsl };
        var user = smtp["Username"];
        if (!string.IsNullOrWhiteSpace(user))
            client.Credentials = new NetworkCredential(user, smtp["Password"]);

        using var message = new MailMessage(from, to, subject, htmlBody) { IsBodyHtml = true };
        await client.SendMailAsync(message, ct);
    }
}
