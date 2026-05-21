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
public class OrdersController : Controller
{
    private readonly AppDbContext _db;
    public OrdersController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(OrderFilter filter)
    {
        var currentManagerId = GetCurrentManagerId();
        var search = filter.Search?.Trim();
        var query = _db.Orders
            .AsNoTracking()
            .Include(x => x.ServiceRequest)!.ThenInclude(x => x!.Client)
            .Include(x => x.PaymentStatus)
            .Include(x => x.ExecutionStatus)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.Services.Contains(search!) ||
                x.ServiceRequest!.Client!.Name.Contains(search!));
        }

        if (currentManagerId.HasValue)
        {
            query = query.Where(x => x.ServiceRequest!.ManagerId == currentManagerId.Value);
        }

        if (filter.PaymentStatusId.HasValue) query = query.Where(x => x.PaymentStatusId == filter.PaymentStatusId);
        if (filter.ExecutionStatusId.HasValue) query = query.Where(x => x.ExecutionStatusId == filter.ExecutionStatusId);
        if (filter.From.HasValue) query = query.Where(x => x.DueDate >= filter.From.Value);
        if (filter.To.HasValue) query = query.Where(x => x.DueDate <= filter.To.Value);
        query = filter.Sort == "amount_desc" ? query.OrderByDescending(x => x.Amount) : query.OrderBy(x => x.DueDate);
        await FillLists();
        ViewBag.Filter = filter;
        return View(await query.ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        await FillLists();
        return View(new Order { DueDate = DateTime.Today.AddDays(7), PaymentStatusId = 1, ExecutionStatusId = 1 });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Order order)
    {
        await ValidateServiceRequestAvailability(order.ServiceRequestId);

        if (!ModelState.IsValid)
        {
            await FillLists();
            return View(order);
        }
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var order = await _db.Orders
            .Include(x => x.ServiceRequest)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (order == null) return NotFound();
        if (!CanManageOrder(order)) return Forbid();

        await FillLists(order.Id);
        return View(order);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Order order)
    {
        if (id != order.Id) return BadRequest();
        await ValidateServiceRequestAvailability(order.ServiceRequestId, order.Id);

        if (!ModelState.IsValid)
        {
            await FillLists(order.Id);
            return View(order);
        }

        var existingOrder = await _db.Orders
            .Include(x => x.ServiceRequest)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (existingOrder == null) return NotFound();
        if (!CanManageOrder(existingOrder)) return Forbid();

        existingOrder.ServiceRequestId = order.ServiceRequestId;
        existingOrder.Services = order.Services;
        existingOrder.Amount = order.Amount;
        existingOrder.DueDate = order.DueDate;
        existingOrder.PaymentStatusId = order.PaymentStatusId;
        existingOrder.ExecutionStatusId = order.ExecutionStatusId;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(x => x.ServiceRequest)!.ThenInclude(x => x!.Client)
            .FirstOrDefaultAsync(x => x.Id == id);
        return order == null ? NotFound() : View(order);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken, Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order != null)
        {
            _db.Orders.Remove(order);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task FillLists(int? currentOrderId = null)
    {
        var usedRequestIds = _db.Orders
            .AsNoTracking()
            .Where(x => !currentOrderId.HasValue || x.Id != currentOrderId.Value)
            .Select(x => x.ServiceRequestId);

        var currentManagerId = GetCurrentManagerId();
        var requestsQuery = _db.ServiceRequests
            .AsNoTracking()
            .Include(x => x.Client)
            .Where(x => !usedRequestIds.Contains(x.Id));

        if (currentManagerId.HasValue)
        {
            requestsQuery = requestsQuery.Where(x => x.ManagerId == currentManagerId.Value);
        }

        ViewBag.Requests = new SelectList(
            await requestsQuery
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new { x.Id, Title = $"#{x.Id} - {x.Client!.Name}" })
                .ToListAsync(),
            "Id",
            "Title");
        ViewBag.PaymentStatuses = new SelectList(
            await _db.OrderPaymentStatuses.AsNoTracking().ToListAsync(),
            "Id",
            "Name");
        ViewBag.ExecutionStatuses = new SelectList(
            await _db.OrderExecutionStatuses.AsNoTracking().ToListAsync(),
            "Id",
            "Name");
    }

    private async Task ValidateServiceRequestAvailability(int serviceRequestId, int? currentOrderId = null)
    {
        if (!await _db.ServiceRequests.AsNoTracking().AnyAsync(x => x.Id == serviceRequestId))
        {
            ModelState.AddModelError(nameof(Order.ServiceRequestId), "Выберите заявку.");
            return;
        }

        var currentManagerId = GetCurrentManagerId();
        if (currentManagerId.HasValue &&
            !await _db.ServiceRequests.AsNoTracking().AnyAsync(x => x.Id == serviceRequestId && x.ManagerId == currentManagerId.Value))
        {
            ModelState.AddModelError(nameof(Order.ServiceRequestId), "Менеджер может создавать заказы только по своим заявкам.");
            return;
        }

        // The database has a unique index too; this check keeps the form error friendly.
        var requestAlreadyHasOrder = await _db.Orders
            .AsNoTracking()
            .AnyAsync(x =>
                x.ServiceRequestId == serviceRequestId &&
                (!currentOrderId.HasValue || x.Id != currentOrderId.Value));

        if (requestAlreadyHasOrder)
        {
            ModelState.AddModelError(nameof(Order.ServiceRequestId), "По выбранной заявке уже создан заказ.");
        }
    }

    private int? GetCurrentManagerId()
    {
        if (!User.IsInRole(UserRoles.Manager))
        {
            return null;
        }

        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;
    }

    private bool CanManageOrder(Order order)
    {
        var currentManagerId = GetCurrentManagerId();
        return !currentManagerId.HasValue || order.ServiceRequest?.ManagerId == currentManagerId.Value;
    }
}
