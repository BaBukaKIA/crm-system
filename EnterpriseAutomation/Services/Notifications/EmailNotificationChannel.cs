using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using EnterpriseAutomation.Services.Security;

namespace EnterpriseAutomation.Services.Notifications;

public sealed class EmailNotificationChannel : INotificationChannel
{
    private readonly NotificationSettings _settings;
    private readonly ITextProtector _textProtector;

    public EmailNotificationChannel(IOptions<NotificationSettings> settings, ITextProtector textProtector)
    {
        _settings = settings.Value;
        _textProtector = textProtector;
    }

    public string Name => "Email";

    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(_settings.EmailHost) &&
        !string.IsNullOrWhiteSpace(_settings.EmailFrom) &&
        !string.IsNullOrWhiteSpace(_settings.EmailTo);

    public async Task<NotificationDeliveryResult> SendAsync(
        NotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return new NotificationDeliveryResult(Name, false, "Email is not configured.");
        }

        using var client = new SmtpClient(_settings.EmailHost!, _settings.EmailPort)
        {
            EnableSsl = _settings.EnableSsl
        };

        var password = _textProtector.TryUnprotect(_settings.EmailPassword);
        if (!string.IsNullOrWhiteSpace(_settings.EmailUsername))
        {
            client.Credentials = new NetworkCredential(_settings.EmailUsername, password);
        }

        using var mail = new MailMessage(_settings.EmailFrom!, _settings.EmailTo!, message.Subject, message.Body)
        {
            IsBodyHtml = false
        };

        await client.SendMailAsync(mail);
        cancellationToken.ThrowIfCancellationRequested();
        return new NotificationDeliveryResult(Name, true, "Sent");
    }
}
