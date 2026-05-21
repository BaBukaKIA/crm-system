namespace EnterpriseAutomation.Services.Notifications;

public sealed record NotificationMessage(
    string Subject,
    string Body,
    string? Recipient = null,
    string? RelatedEntity = null,
    string? RelatedEntityKey = null);

public sealed record NotificationDeliveryResult(
    string Channel,
    bool IsSuccess,
    string Details);

public interface INotificationChannel
{
    string Name { get; }

    bool IsEnabled { get; }

    Task<NotificationDeliveryResult> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}

public interface INotificationDispatcher
{
    Task<IReadOnlyList<NotificationDeliveryResult>> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}
