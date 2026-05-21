using EnterpriseAutomation.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAutomation.Controllers.Api;

[Authorize]
[ApiController]
[Route("api/orders")]
public class OrdersApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public OrdersApiController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get(int? paymentStatusId, int? executionStatusId) =>
        Ok(await _db.Orders
            .AsNoTracking()
            .Include(x => x.ServiceRequest)!.ThenInclude(x => x!.Client)
            .Include(x => x.PaymentStatus)
            .Include(x => x.ExecutionStatus)
            .Where(x => !paymentStatusId.HasValue || x.PaymentStatusId == paymentStatusId)
            .Where(x => !executionStatusId.HasValue || x.ExecutionStatusId == executionStatusId)
            .ToListAsync());
}
