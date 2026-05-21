using EnterpriseAutomation.ViewModels;

namespace EnterpriseAutomation.Services.Dashboard;

public interface IDashboardSnapshotService
{
    Task<DashboardSnapshot> BuildAsync(DashboardQuery? query = null, CancellationToken cancellationToken = default);

    Task<DashboardSnapshot> MoveTaskAsync(DashboardTaskMoveRequest request, CancellationToken cancellationToken = default);
}
