using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace EnterpriseAutomation.Services.Notifications;

public sealed class TeamsWebhookNotificationChannel : INotificationChannel
{
    private readonly NotificationSettings _settings;

    public TeamsWebhookNotificationChannel(IOptions<NotificationSettings> settings)
    {
        _settings = settings.Value;
    }

    public string Name => "Teams";

    public bool IsEnabled => !string.IsNullOrWhiteSpace(_settings.TeamsWebhookUrl);

    public async Task<NotificationDeliveryResult> SendAsync(
        NotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return new NotificationDeliveryResult(Name, false, "Teams webhook is not configured.");
        }

        using var client = new HttpClient();
        var payload = new
        {
            text = $"{message.Subject}\n{message.Body}"
        };

        using var response = await client.PostAsJsonAsync(_settings.TeamsWebhookUrl!, payload, cancellationToken);
        return new NotificationDeliveryResult(Name, response.IsSuccessStatusCode, $"HTTP {(int)response.StatusCode}");
    }
}
