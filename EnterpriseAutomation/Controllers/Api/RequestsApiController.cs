using EnterpriseAutomation.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAutomation.Controllers.Api;

[Authorize]
[ApiController]
[Route("api/requests")]
public class RequestsApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public RequestsApiController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get(int? statusId, int? managerId) =>
        Ok(await _db.ServiceRequests.Include(x => x.Client).Include(x => x.RequestStatus).Include(x => x.Manager)
            .Where(x => !statusId.HasValue || x.RequestStatusId == statusId)
            .Where(x => !managerId.HasValue || x.ManagerId == managerId)
            .ToListAsync());
}
