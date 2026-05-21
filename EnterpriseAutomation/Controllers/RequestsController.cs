using System.Security.Claims;
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
        var currentManagerId = GetCurrentManagerId();
        var search = filter.Search?.Trim();
        var query = _db.ServiceRequests
            .AsNoTracking()
            .Include(x => x.Client)
            .Include(x => x.RequestStatus)
            .Include(x => x.Manager)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Description.Contains(search!) || x.Client!.Name.Contains(search!));
        }

        if (currentManagerId.HasValue)
        {
            filter.ManagerId = currentManagerId.Value;
            query = query.Where(x => x.ManagerId == currentManagerId.Value);
        }

        if (filter.StatusId.HasValue) query = query.Where(x => x.RequestStatusId == filter.StatusId);
        if (!currentManagerId.HasValue && filter.ManagerId.HasValue) query = query.Where(x => x.ManagerId == filter.ManagerId);
        if (filter.From.HasValue) query = query.Where(x => x.CreatedAt >= filter.From.Value);
        if (filter.To.HasValue) query = query.Where(x => x.CreatedAt <= filter.To.Value);
        query = filter.Sort == "date" ? query.OrderBy(x => x.CreatedAt) : query.OrderByDescending(x => x.CreatedAt);
        await FillLists(filter.StatusId, filter.ManagerId);
        ViewBag.Filter = filter;
        return View(await query.ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        var request = new ServiceRequest
        {
            CreatedAt = DateTime.Today,
            RequestStatusId = 1,
            ManagerId = GetCurrentManagerId() ?? 0
        };

        await FillLists(request.RequestStatusId, request.ManagerId);
        return View(request);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceRequest request)
    {
        var currentManagerId = GetCurrentManagerId();
        if (currentManagerId.HasValue)
        {
            request.ManagerId = currentManagerId.Value;
        }

        if (!ModelState.IsValid)
        {
            await FillLists(request.RequestStatusId, request.ManagerId);
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
        if (!CanManageRequest(request)) return Forbid();

        await FillLists(request.RequestStatusId, request.ManagerId);
        return View(request);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ServiceRequest request)
    {
        if (id != request.Id) return BadRequest();
        var existingRequest = await _db.ServiceRequests.FindAsync(id);
        if (existingRequest == null) return NotFound();
        if (!CanManageRequest(existingRequest)) return Forbid();

        var currentManagerId = GetCurrentManagerId();
        if (currentManagerId.HasValue)
        {
            request.ManagerId = currentManagerId.Value;
        }

        if (!ModelState.IsValid)
        {
            await FillLists(request.RequestStatusId, request.ManagerId);
            return View(request);
        }

        existingRequest.ClientId = request.ClientId;
        existingRequest.CreatedAt = request.CreatedAt;
        existingRequest.Description = request.Description;
        existingRequest.RequestStatusId = request.RequestStatusId;
        existingRequest.ManagerId = request.ManagerId;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var request = await _db.ServiceRequests
            .AsNoTracking()
            .Include(x => x.Client)
            .FirstOrDefaultAsync(x => x.Id == id);
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

    private async Task FillLists(int? selectedStatusId = null, int? selectedManagerId = null)
    {
        var currentManagerId = GetCurrentManagerId();
        ViewBag.Clients = new SelectList(
            await _db.Clients.AsNoTracking().OrderBy(x => x.Name).ToListAsync(),
            "Id",
            "Name");
        ViewBag.Statuses = new SelectList(
            await _db.RequestStatuses.AsNoTracking().ToListAsync(),
            "Id",
            "Name",
            selectedStatusId);

        var managersQuery = _db.Users.AsNoTracking().Where(x => x.Role == UserRoles.Manager);
        if (currentManagerId.HasValue)
        {
            selectedManagerId = currentManagerId.Value;
            managersQuery = managersQuery.Where(x => x.Id == currentManagerId.Value);
        }

        var managers = await managersQuery.OrderBy(x => x.FullName).ToListAsync();
        ViewBag.Managers = new SelectList(
            managers,
            "Id",
            "FullName",
            selectedManagerId);
        ViewBag.IsCurrentUserManager = currentManagerId.HasValue;
        ViewBag.CurrentManagerName = managers.FirstOrDefault(x => x.Id == selectedManagerId)?.FullName ?? User.Identity?.Name ?? "Менеджер";
    }

    private int? GetCurrentManagerId()
    {
        if (!User.IsInRole(UserRoles.Manager))
        {
            return null;
        }

        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;
    }

    private bool CanManageRequest(ServiceRequest request)
    {
        var currentManagerId = GetCurrentManagerId();
        return !currentManagerId.HasValue || request.ManagerId == currentManagerId.Value;
    }
}
