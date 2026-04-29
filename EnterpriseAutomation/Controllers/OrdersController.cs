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
        var query = _db.Orders.Include(x => x.ServiceRequest)!.ThenInclude(x => x!.Client)
            .Include(x => x.PaymentStatus).Include(x => x.ExecutionStatus).AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.Search)) query = query.Where(x => x.Services.Contains(filter.Search) || x.ServiceRequest!.Client!.Name.Contains(filter.Search));
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
        if (!await _db.ServiceRequests.AnyAsync(x => x.Id == order.ServiceRequestId))
        {
            ModelState.AddModelError(nameof(Order.ServiceRequestId), "Выберите заявку.");
        }
        else if (await _db.Orders.AnyAsync(x => x.ServiceRequestId == order.ServiceRequestId))
        {
            ModelState.AddModelError(nameof(Order.ServiceRequestId), "По выбранной заявке уже создан заказ.");
        }

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
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound();
        await FillLists(order.Id);
        return View(order);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Order order)
    {
        if (id != order.Id) return BadRequest();
        if (!await _db.ServiceRequests.AnyAsync(x => x.Id == order.ServiceRequestId))
        {
            ModelState.AddModelError(nameof(Order.ServiceRequestId), "Выберите заявку.");
        }
        else if (await _db.Orders.AnyAsync(x => x.Id != order.Id && x.ServiceRequestId == order.ServiceRequestId))
        {
            ModelState.AddModelError(nameof(Order.ServiceRequestId), "По выбранной заявке уже создан заказ.");
        }

        if (!ModelState.IsValid)
        {
            await FillLists(order.Id);
            return View(order);
        }

        var existingOrder = await _db.Orders.FindAsync(id);
        if (existingOrder == null) return NotFound();

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
        var order = await _db.Orders.Include(x => x.ServiceRequest)!.ThenInclude(x => x!.Client).FirstOrDefaultAsync(x => x.Id == id);
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
            .Where(x => !currentOrderId.HasValue || x.Id != currentOrderId.Value)
            .Select(x => x.ServiceRequestId);

        ViewBag.Requests = new SelectList(
            await _db.ServiceRequests
                .Include(x => x.Client)
                .Where(x => !usedRequestIds.Contains(x.Id))
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new { x.Id, Title = $"#{x.Id} - {x.Client!.Name}" })
                .ToListAsync(),
            "Id",
            "Title");
        ViewBag.PaymentStatuses = new SelectList(await _db.OrderPaymentStatuses.ToListAsync(), "Id", "Name");
        ViewBag.ExecutionStatuses = new SelectList(await _db.OrderExecutionStatuses.ToListAsync(), "Id", "Name");
    }
}
