using EnterpriseAutomation.Data;
using EnterpriseAutomation.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAutomation.Controllers.Api;

[Authorize]
[ApiController]
[Route("api/clients")]
public class ClientsApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public ClientsApiController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get(string? search)
    {
        var searchTerm = search?.Trim();
        var query = _db.Clients.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x =>
                x.Name.Contains(searchTerm!) ||
                (x.Email != null && x.Email.Contains(searchTerm!)));
        }

        return Ok(await query.ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) =>
        await _db.Clients.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) is { } client
            ? Ok(client)
            : NotFound(new { message = "Клиент не найден" });

    [HttpPost]
    public async Task<IActionResult> Create(Client client)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = client.Id }, client);
    }
}
