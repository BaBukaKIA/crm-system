using EnterpriseAutomation.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAutomation.Controllers.Api;

[Authorize]
[ApiController]
[Route("api/reports")]
public class ReportsApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public ReportsApiController(AppDbContext db) => _db = db;

    [HttpGet("orders")]
    public async Task<IActionResult> Orders(DateTime? from, DateTime? to)
    {
        var fromDate = (from ?? DateTime.Today.AddMonths(-1)).Date;
        var toDate = (to ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

        var orders = await _db.Orders
            .Include(x => x.ServiceRequest)!.ThenInclude(x => x!.Client)
            .Include(x => x.PaymentStatus)
            .Include(x => x.ExecutionStatus)
            .Where(x => x.DueDate >= fromDate && x.DueDate <= toDate)
            .OrderBy(x => x.DueDate)
            .Select(x => new
            {
                orderId = x.Id,
                clientName = x.ServiceRequest!.Client!.Name,
                services = x.Services,
                amount = x.Amount,
                dueDate = x.DueDate,
                paymentStatus = x.PaymentStatus!.Name,
                executionStatus = x.ExecutionStatus!.Name
            })
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("requests-by-status")]
    public async Task<IActionResult> RequestsByStatus(int? statusId)
    {
        var requests = await _db.ServiceRequests
            .Include(x => x.RequestStatus)
            .Where(x => !statusId.HasValue || x.RequestStatusId == statusId)
            .GroupBy(x => new { x.RequestStatusId, Status = x.RequestStatus!.Name })
            .Select(x => new
            {
                statusId = x.Key.RequestStatusId,
                status = x.Key.Status,
                count = x.Count()
            })
            .OrderByDescending(x => x.count)
            .ThenBy(x => x.status)
            .ToListAsync();

        return Ok(requests);
    }

    [HttpGet("clients")]
    public async Task<IActionResult> Clients(int take = 10)
    {
        take = Math.Clamp(take, 1, 100);

        var clients = await _db.Clients
            .Select(x => new
            {
                clientId = x.Id,
                clientName = x.Name,
                ordersCount = x.Requests.Count(r => r.Order != null),
                totalAmount = x.Requests.Where(r => r.Order != null).Sum(r => (decimal?)r.Order!.Amount) ?? 0
            })
            .OrderByDescending(x => x.totalAmount)
            .ThenByDescending(x => x.ordersCount)
            .ThenBy(x => x.clientName)
            .Take(take)
            .ToListAsync();

        return Ok(clients);
    }

    [HttpGet("top-clients")]
    public async Task<IActionResult> TopClients() =>
        Ok(await _db.Clients
            .Select(x => new
            {
                client = x.Name,
                orders = x.Requests.Count(r => r.Order != null),
                total = x.Requests.Where(r => r.Order != null).Sum(r => (decimal?)r.Order!.Amount) ?? 0
            })
            .OrderByDescending(x => x.total)
            .Take(5)
            .ToListAsync());
}
