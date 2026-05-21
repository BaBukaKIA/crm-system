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
        var period = NormalizePeriod(from, to);
        if (period.From > period.To)
        {
            return BadRequest("Дата начала не может быть позже даты окончания.");
        }

        var orders = await _db.Orders
            .AsNoTracking()
            .Include(x => x.ServiceRequest)!.ThenInclude(x => x!.Client)
            .Include(x => x.PaymentStatus)
            .Include(x => x.ExecutionStatus)
            .Where(x => x.DueDate >= period.From && x.DueDate <= period.To)
            .OrderBy(x => x.DueDate)
            .Select(x => new
            {
                orderId = x.Id,
                clientName = x.ServiceRequest!.Client!.Name,
                services = x.Services,
                amount = x.Amount,
                dueDate = x.DueDate,
                isOverdue = x.ExecutionStatusId != 3 && x.DueDate.Date < DateTime.Today,
                paymentStatusId = x.PaymentStatusId,
                paymentStatus = x.PaymentStatus!.Name,
                executionStatusId = x.ExecutionStatusId,
                executionStatus = x.ExecutionStatus!.Name
            })
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("requests-by-status")]
    public async Task<IActionResult> RequestsByStatus(int? statusId, DateTime? from, DateTime? to)
    {
        var period = NormalizePeriod(from, to);
        if (period.From > period.To)
        {
            return BadRequest("Дата начала не может быть позже даты окончания.");
        }

        var requests = await _db.ServiceRequests
            .AsNoTracking()
            .Include(x => x.RequestStatus)
            .Where(x => x.CreatedAt >= period.From && x.CreatedAt <= period.To)
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
    public async Task<IActionResult> Clients(int take = 10, DateTime? from = null, DateTime? to = null)
    {
        take = Math.Clamp(take, 1, 100);
        var period = NormalizePeriod(from, to);
        if (period.From > period.To)
        {
            return BadRequest("Дата начала не может быть позже даты окончания.");
        }

        var clients = await _db.Clients
            .AsNoTracking()
            .Select(x => new
            {
                clientId = x.Id,
                clientName = x.Name,
                ordersCount = x.Requests.Count(r =>
                    r.Order != null &&
                    r.Order.DueDate >= period.From &&
                    r.Order.DueDate <= period.To),
                totalAmount = x.Requests
                    .Where(r =>
                        r.Order != null &&
                        r.Order.DueDate >= period.From &&
                        r.Order.DueDate <= period.To)
                    .Sum(r => (decimal?)r.Order!.Amount) ?? 0
            })
            .Where(x => x.ordersCount > 0)
            .OrderByDescending(x => x.totalAmount)
            .ThenByDescending(x => x.ordersCount)
            .ThenBy(x => x.clientName)
            .Take(take)
            .ToListAsync();

        return Ok(clients);
    }

    [HttpGet("top-clients")]
    public async Task<IActionResult> TopClients(DateTime? from, DateTime? to)
    {
        var period = NormalizePeriod(from, to);
        if (period.From > period.To)
        {
            return BadRequest("Дата начала не может быть позже даты окончания.");
        }

        return Ok(await _db.Clients
            .AsNoTracking()
            .Select(x => new
            {
                client = x.Name,
                orders = x.Requests.Count(r =>
                    r.Order != null &&
                    r.Order.DueDate >= period.From &&
                    r.Order.DueDate <= period.To),
                total = x.Requests
                    .Where(r =>
                        r.Order != null &&
                        r.Order.DueDate >= period.From &&
                        r.Order.DueDate <= period.To)
                    .Sum(r => (decimal?)r.Order!.Amount) ?? 0
            })
            .Where(x => x.orders > 0)
            .OrderByDescending(x => x.total)
            .ThenBy(x => x.client)
            .Take(5)
            .ToListAsync());
    }

    private static (DateTime From, DateTime To) NormalizePeriod(DateTime? from, DateTime? to)
    {
        return (
            (from ?? DateTime.Today.AddMonths(-1)).Date,
            (to ?? DateTime.Today).Date.AddDays(1).AddTicks(-1));
    }
}
