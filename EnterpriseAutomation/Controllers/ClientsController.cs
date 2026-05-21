using EnterpriseAutomation.Data;
using EnterpriseAutomation.Models;
using EnterpriseAutomation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAutomation.Controllers;

[Authorize]
public class ClientsController : Controller
{
    private readonly AppDbContext _db;
    public ClientsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(ClientFilter filter)
    {
        var search = filter.Search?.Trim();
        var query = _db.Clients.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.Name.Contains(search!) ||
                (x.Phone != null && x.Phone.Contains(search!)) ||
                (x.Email != null && x.Email.Contains(search!)));
        }

        query = filter.Sort == "email" ? query.OrderBy(x => x.Email) : query.OrderBy(x => x.Name);
        ViewBag.Filter = filter;
        return View(await query.ToListAsync());
    }

    public IActionResult Create() => View(new Client());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Client client)
    {
        if (!ModelState.IsValid) return View(client);
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var client = await _db.Clients.FindAsync(id);
        return client == null ? NotFound() : View(client);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Client client)
    {
        if (id != client.Id) return BadRequest();
        if (!ModelState.IsValid) return View(client);
        _db.Update(client);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var client = await _db.Clients.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return client == null ? NotFound() : View(client);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken, Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var client = await _db.Clients.FindAsync(id);
        if (client != null)
        {
            _db.Clients.Remove(client);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
