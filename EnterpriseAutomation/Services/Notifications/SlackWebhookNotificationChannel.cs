using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace EnterpriseAutomation.Services.Notifications;

public sealed class SlackWebhookNotificationChannel : INotificationChannel
{
    private readonly NotificationSettings _settings;

    public SlackWebhookNotificationChannel(IOptions<NotificationSettings> settings)
    {
        _settings = settings.Value;
    }

    public string Name => "Slack";

    public bool IsEnabled => !string.IsNullOrWhiteSpace(_settings.SlackWebhookUrl);

    public async Task<NotificationDeliveryResult> SendAsync(
        NotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return new NotificationDeliveryResult(Name, false, "Slack webhook is not configured.");
        }

        using var client = new HttpClient();
        var payload = new
        {
            text = $"*{message.Subject}*\n{message.Body}"
        };

        using var response = await client.PostAsJsonAsync(_settings.SlackWebhookUrl!, payload, cancellationToken);
        return new NotificationDeliveryResult(Name, response.IsSuccessStatusCode, $"HTTP {(int)response.StatusCode}");
    }
}
