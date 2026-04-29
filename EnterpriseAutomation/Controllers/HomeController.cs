using System.Diagnostics;
using EnterpriseAutomation.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnterpriseAutomation.Models;
using EnterpriseAutomation.ViewModels;

namespace EnterpriseAutomation.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var model = new DashboardViewModel();

        if (User.Identity?.IsAuthenticated == true)
        {
            model.ClientsCount = await _db.Clients.CountAsync();
            model.RequestsCount = await _db.ServiceRequests.CountAsync();
            model.OrdersCount = await _db.Orders.CountAsync();
            model.OrdersAmount = await _db.Orders.SumAsync(x => (decimal?)x.Amount) ?? 0;
            model.UnpaidOrdersCount = await _db.Orders.CountAsync(x => x.PaymentStatusId != 3);
        }

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
