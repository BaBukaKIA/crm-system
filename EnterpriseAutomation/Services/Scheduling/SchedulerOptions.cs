namespace EnterpriseAutomation.Services.Scheduling;

public class SchedulerOptions
{
    public int PollSeconds { get; set; } = 60;

    public int ReminderDays { get; set; } = 3;
}
