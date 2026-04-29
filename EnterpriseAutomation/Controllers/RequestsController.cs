using EnterpriseAutomation.Data;
using EnterpriseAutomation.Models;
using EnterpriseAutomation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAutomation.Controllers;

[Authorize]
public class RequestsController : Controller
{
    private readonly AppDbContext _db;
    public RequestsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(RequestFilter filter)
    {
        var query = _db.ServiceRequests.Include(x => x.Client).Include(x => x.RequestStatus).Include(x => x.Manager).AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.Search)) query = query.Where(x => x.Description.Contains(filter.Search) || x.Client!.Name.Contains(filter.Search));
        if (filter.StatusId.HasValue) query = query.Where(x => x.RequestStatusId == filter.StatusId);
        if (filter.ManagerId.HasValue) query = query.Where(x => x.ManagerId == filter.ManagerId);
        if (filter.From.HasValue) query = query.Where(x => x.CreatedAt >= filter.From.Value);
        if (filter.To.HasValue) query = query.Where(x => x.CreatedAt <= filter.To.Value);
        query = filter.Sort == "date" ? query.OrderBy(x => x.CreatedAt) : query.OrderByDescending(x => x.CreatedAt);
        await FillLists();
        ViewBag.Filter = filter;
        return View(await query.ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        await FillLists();
        return View(new ServiceRequest { CreatedAt = DateTime.Today, RequestStatusId = 1 });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceRequest request)
    {
        if (!ModelState.IsValid)
        {
            await FillLists();
            return View(request);
        }
        _db.ServiceRequests.Add(request);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var request = await _db.ServiceRequests.FindAsync(id);
        if (request == null) return NotFound();
        await FillLists();
        return View(request);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ServiceRequest request)
    {
        if (id != request.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            await FillLists();
            return View(request);
        }
        _db.Update(request);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var request = await _db.ServiceRequests.Include(x => x.Client).FirstOrDefaultAsync(x => x.Id == id);
        return request == null ? NotFound() : View(request);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken, Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var request = await _db.ServiceRequests.FindAsync(id);
        if (request != null)
        {
            _db.ServiceRequests.Remove(request);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task FillLists()
    {
        ViewBag.Clients = new SelectList(await _db.Clients.OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
        ViewBag.Statuses = new SelectList(await _db.RequestStatuses.ToListAsync(), "Id", "Name");
        ViewBag.Managers = new SelectList(await _db.Users.Where(x => x.Role == UserRoles.Manager).OrderBy(x => x.FullName).ToListAsync(), "Id", "FullName");
    }
}
