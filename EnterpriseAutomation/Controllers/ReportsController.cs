using EnterpriseAutomation.Data;
using EnterpriseAutomation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EnterpriseAutomation.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly AppDbContext _db;
    public ReportsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(DateTime? from, DateTime? to)
    {
        var model = new ReportsViewModel
        {
            From = from ?? DateTime.Today.AddMonths(-1),
            To = to ?? DateTime.Today
        };

        ViewBag.Statuses = new SelectList(await _db.RequestStatuses.OrderBy(x => x.Name).ToListAsync(), "Id", "Name");

        return View(model);
    }
}
