using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlowASP.Data;
using JurisFlowASP.Models;
using JurisFlowASP.ViewModels;
using JurisFlowASP.Services;

namespace JurisFlowASP.Controllers;

[Authorize]
public class ClientsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IAuthService _authService;

    public ClientsController(ApplicationDbContext context, IAuditService auditService, IAuthService authService)
    {
        _context = context;
        _auditService = auditService;
        _authService = authService;
    }

    // GET: Clients
    public async Task<IActionResult> Index(string? status = null, string? type = null, string? search = null)
    {
        var query = _context.Clients.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status == status);

        if (!string.IsNullOrEmpty(type))
            query = query.Where(c => c.Type == type);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(c => c.Name.Contains(search) || c.Email.Contains(search) || (c.Company != null && c.Company.Contains(search)));

        var clients = await query.OrderBy(c => c.Name).ToListAsync();

        ViewBag.CurrentStatus = status;
        ViewBag.CurrentType = type;
        ViewBag.Search = search;

        return View(clients);
    }

    // GET: Clients/Details/5
    public async Task<IActionResult> Details(string id)
    {
        var client = await _context.Clients
            .Include(c => c.Matters)
            .Include(c => c.Invoices)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (client == null)
            return NotFound();

        return View(client);
    }

    // GET: Clients/Create
    public IActionResult Create()
    {
        return View(new ClientCreateViewModel());
    }

    // POST: Clients/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClientCreateViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (await _context.Clients.AnyAsync(c => c.Email == model.Email))
        {
            ModelState.AddModelError("Email", "Bu e-posta adresi zaten kullanılıyor.");
            return View(model);
        }

        var client = new Client
        {
            Name = model.Name,
            Email = model.Email,
            Phone = model.Phone,
            Mobile = model.Mobile,
            Company = model.Company,
            Type = model.Type,
            Status = model.Status,
            Address = model.Address,
            City = model.City,
            State = model.State,
            ZipCode = model.ZipCode,
            Country = model.Country,
            TaxId = model.TaxId,
            Notes = model.Notes,
            PortalAccess = model.PortalAccess,
            PortalPasswordHash = !string.IsNullOrEmpty(model.PortalPassword) ? _authService.HashPassword(model.PortalPassword) : null
        };

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("CREATE", "Client", client.Id, newValues: client);

        TempData["Success"] = "Müvekkil başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Details), new { id = client.Id });
    }

    // GET: Clients/Edit/5
    public async Task<IActionResult> Edit(string id)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client == null)
            return NotFound();

        var model = new ClientCreateViewModel
        {
            Name = client.Name,
            Email = client.Email,
            Phone = client.Phone,
            Mobile = client.Mobile,
            Company = client.Company,
            Type = client.Type,
            Status = client.Status,
            Address = client.Address,
            City = client.City,
            State = client.State,
            ZipCode = client.ZipCode,
            Country = client.Country,
            TaxId = client.TaxId,
            Notes = client.Notes,
            PortalAccess = client.PortalAccess
        };

        return View(model);
    }

    // POST: Clients/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, ClientCreateViewModel model)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client == null)
            return NotFound();

        if (!ModelState.IsValid)
            return View(model);

        if (await _context.Clients.AnyAsync(c => c.Email == model.Email && c.Id != id))
        {
            ModelState.AddModelError("Email", "Bu e-posta adresi zaten kullanılıyor.");
            return View(model);
        }

        var oldValues = new { client.Name, client.Email, client.Status };

        client.Name = model.Name;
        client.Email = model.Email;
        client.Phone = model.Phone;
        client.Mobile = model.Mobile;
        client.Company = model.Company;
        client.Type = model.Type;
        client.Status = model.Status;
        client.Address = model.Address;
        client.City = model.City;
        client.State = model.State;
        client.ZipCode = model.ZipCode;
        client.Country = model.Country;
        client.TaxId = model.TaxId;
        client.Notes = model.Notes;
        client.PortalAccess = model.PortalAccess;
        client.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(model.PortalPassword))
            client.PortalPasswordHash = _authService.HashPassword(model.PortalPassword);

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("UPDATE", "Client", client.Id, oldValues: oldValues, newValues: model);

        TempData["Success"] = "Müvekkil başarıyla güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: Clients/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client == null)
            return NotFound();

        await _auditService.LogAsync("DELETE", "Client", id, oldValues: client);

        _context.Clients.Remove(client);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Müvekkil başarıyla silindi.";
        return RedirectToAction(nameof(Index));
    }
}
