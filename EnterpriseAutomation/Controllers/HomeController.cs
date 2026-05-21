using System.Diagnostics;
using EnterpriseAutomation.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnterpriseAutomation.Models;
using EnterpriseAutomation.Services.Dashboard;
using EnterpriseAutomation.ViewModels;

namespace EnterpriseAutomation.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction("Login", "Account");
        }

        var model = new DashboardViewModel();

        var palette = new[]
        {
            "#2563eb",
            "#0f766e",
            "#7c3aed",
            "#ea580c",
            "#16a34a",
            "#0284c7"
        };

        var requestStatuses = await _db.RequestStatuses.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
        var executionStatuses = await _db.OrderExecutionStatuses.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
        var requests = await _db.ServiceRequests
            .AsNoTracking()
            .Include(x => x.Client)
            .Include(x => x.RequestStatus)
            .Include(x => x.Manager)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
        var orders = await _db.Orders
            .AsNoTracking()
            .Include(x => x.ServiceRequest)!.ThenInclude(x => x!.Client)
            .Include(x => x.PaymentStatus)
            .Include(x => x.ExecutionStatus)
            .OrderByDescending(x => x.DueDate)
            .ToListAsync();

        var metrics = new DashboardMetrics(
            ClientsCount: await _db.Clients.CountAsync(),
            RequestsCount: requests.Count,
            OrdersCount: orders.Count,
            RevenueAmount: orders.Sum(x => x.Amount),
            OpenRequestsCount: requests.Count(x => x.RequestStatusId != 3),
            ActiveOrdersCount: orders.Count(x => x.ExecutionStatusId != 3),
            UnpaidOrdersCount: orders.Count(x => x.PaymentStatusId != 3),
            OverdueOrdersCount: orders.Count(x => x.ExecutionStatusId != 3 && x.DueDate.Date < DateTime.Today));

        var requestStatusSlices = requestStatuses
            .Select((status, index) => new DashboardChartSlice(
                status.Id,
                status.Name,
                requests.Count(x => x.RequestStatusId == status.Id),
                palette[index % palette.Length]))
            .ToList();

        var executionStatusSlices = executionStatuses
            .Select((status, index) => new DashboardChartSlice(
                status.Id,
                status.Name,
                orders.Count(x => x.ExecutionStatusId == status.Id),
                palette[(index + 2) % palette.Length]))
            .ToList();

        var revenueTrend = orders
            .GroupBy(x => x.DueDate.Date)
            .OrderBy(x => x.Key)
            .Select(x => new DashboardTrendPoint(x.Key.ToString("dd.MM"), x.Sum(order => order.Amount)))
            .ToList();

        if (revenueTrend.Count > 8)
        {
            revenueTrend = revenueTrend.TakeLast(8).ToList();
        }

        var topClients = orders
            .GroupBy(x => x.ServiceRequest?.Client?.Name ?? "Клиент не указан")
            .Select(x => new DashboardTopClientItem(
                x.Key,
                x.Count(),
                x.Sum(order => order.Amount)))
            .OrderByDescending(x => x.TotalAmount)
            .ThenBy(x => x.Name)
            .Take(5)
            .ToList();

        model.IsAuthenticated = true;
        model.Snapshot = new DashboardSnapshot(
            metrics,
            requestStatusSlices,
            executionStatusSlices,
            revenueTrend,
            Array.Empty<DashboardBoardColumn>(),
            Array.Empty<DashboardBoardColumn>(),
            Array.Empty<DashboardActivityItem>(),
            Array.Empty<DashboardJobRunItem>(),
            topClients,
            DateTime.UtcNow)
        {
            AttentionItems = DashboardAttentionBuilder.Build(requests, orders)
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
