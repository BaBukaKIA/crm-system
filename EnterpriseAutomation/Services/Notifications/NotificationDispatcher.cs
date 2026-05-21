using Microsoft.Extensions.Options;

namespace EnterpriseAutomation.Services.Notifications;

public sealed class NotificationDispatcher : INotificationDispatcher
{
    private readonly IReadOnlyList<INotificationChannel> _channels;

    public NotificationDispatcher(IEnumerable<INotificationChannel> channels)
    {
        _channels = channels.ToList();
    }

    public async Task<IReadOnlyList<NotificationDeliveryResult>> SendAsync(
        NotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        var results = new List<NotificationDeliveryResult>();

        foreach (var channel in _channels.Where(x => x.IsEnabled))
        {
            results.Add(await channel.SendAsync(message, cancellationToken));
        }

        return results;
    }
}
