namespace EnterpriseAutomation.Services.Notifications;

public class NotificationSettings
{
    public string? SlackWebhookUrl { get; set; }

    public string? TeamsWebhookUrl { get; set; }

    public string? EmailHost { get; set; }

    public int EmailPort { get; set; } = 587;

    public string? EmailFrom { get; set; }

    public string? EmailTo { get; set; }

    public string? EmailUsername { get; set; }

    public string? EmailPassword { get; set; }

    public bool EnableSsl { get; set; } = true;
}
