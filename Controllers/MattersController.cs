using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlowASP.Data;
using JurisFlowASP.Models;
using JurisFlowASP.ViewModels;
using JurisFlowASP.Services;
using System.Security.Claims;

namespace JurisFlowASP.Controllers;

[Authorize]
public class MattersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public MattersController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    // GET: Matters
    public async Task<IActionResult> Index(string? status = null, string? practiceArea = null, string? search = null)
    {
        var query = _context.Matters
            .Include(m => m.Client)
            .Include(m => m.Tasks)
            .Include(m => m.Documents)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(m => m.Status == status);

        if (!string.IsNullOrEmpty(practiceArea))
            query = query.Where(m => m.PracticeArea == practiceArea);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(m => m.Name.Contains(search) || m.CaseNumber.Contains(search));

        var matters = await query
            .OrderByDescending(m => m.OpenDate)
            .Select(m => new MatterListViewModel
            {
                Id = m.Id,
                CaseNumber = m.CaseNumber,
                Name = m.Name,
                ClientName = m.Client != null ? m.Client.Name : "",
                PracticeArea = m.PracticeArea,
                Status = m.Status,
                ResponsibleAttorney = m.ResponsibleAttorney,
                OpenDate = m.OpenDate,
                BillableRate = m.BillableRate,
                TaskCount = m.Tasks.Count,
                DocumentCount = m.Documents.Count
            })
            .ToListAsync();

        ViewBag.Clients = await _context.Clients.OrderBy(c => c.Name).ToListAsync();
        ViewBag.CurrentStatus = status;
        ViewBag.CurrentPracticeArea = practiceArea;
        ViewBag.Search = search;

        return View(matters);
    }

    // GET: Matters/Details/5
    public async Task<IActionResult> Details(string id)
    {
        var matter = await _context.Matters
            .Include(m => m.Client)
            .Include(m => m.Tasks)
            .Include(m => m.Documents)
            .Include(m => m.TimeEntries)
            .Include(m => m.Expenses)
            .Include(m => m.Events)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (matter == null)
            return NotFound();

        await _auditService.LogAsync("VIEW", "Matter", id);
        return View(matter);
    }

    // GET: Matters/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.Clients = await _context.Clients.Where(c => c.Status == "Active").OrderBy(c => c.Name).ToListAsync();
        ViewBag.Users = await _context.Users.OrderBy(u => u.Name).ToListAsync();
        return View(new MatterCreateViewModel());
    }

    // POST: Matters/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MatterCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Clients = await _context.Clients.Where(c => c.Status == "Active").OrderBy(c => c.Name).ToListAsync();
            ViewBag.Users = await _context.Users.OrderBy(u => u.Name).ToListAsync();
            return View(model);
        }

        var matter = new Matter
        {
            CaseNumber = model.CaseNumber,
            Name = model.Name,
            ClientId = model.ClientId,
            PracticeArea = model.PracticeArea,
            Status = model.Status,
            FeeStructure = model.FeeStructure,
            ResponsibleAttorney = model.ResponsibleAttorney,
            BillableRate = model.BillableRate,
            TrustBalance = model.TrustBalance,
            OpenDate = DateTime.UtcNow
        };

        _context.Matters.Add(matter);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("CREATE", "Matter", matter.Id, newValues: matter);

        TempData["Success"] = "Dava başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Details), new { id = matter.Id });
    }

    // GET: Matters/Edit/5
    public async Task<IActionResult> Edit(string id)
    {
        var matter = await _context.Matters.FindAsync(id);
        if (matter == null)
            return NotFound();

        ViewBag.Clients = await _context.Clients.OrderBy(c => c.Name).ToListAsync();
        ViewBag.Users = await _context.Users.OrderBy(u => u.Name).ToListAsync();

        var model = new MatterCreateViewModel
        {
            CaseNumber = matter.CaseNumber,
            Name = matter.Name,
            ClientId = matter.ClientId,
            PracticeArea = matter.PracticeArea,
            Status = matter.Status,
            FeeStructure = matter.FeeStructure,
            ResponsibleAttorney = matter.ResponsibleAttorney,
            BillableRate = matter.BillableRate,
            TrustBalance = matter.TrustBalance
        };

        return View(model);
    }

    // POST: Matters/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, MatterCreateViewModel model)
    {
        var matter = await _context.Matters.FindAsync(id);
        if (matter == null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.Clients = await _context.Clients.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Users = await _context.Users.OrderBy(u => u.Name).ToListAsync();
            return View(model);
        }

        var oldValues = new { matter.CaseNumber, matter.Name, matter.Status };

        matter.CaseNumber = model.CaseNumber;
        matter.Name = model.Name;
        matter.ClientId = model.ClientId;
        matter.PracticeArea = model.PracticeArea;
        matter.Status = model.Status;
        matter.FeeStructure = model.FeeStructure;
        matter.ResponsibleAttorney = model.ResponsibleAttorney;
        matter.BillableRate = model.BillableRate;
        matter.TrustBalance = model.TrustBalance;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("UPDATE", "Matter", matter.Id, oldValues: oldValues, newValues: model);

        TempData["Success"] = "Dava başarıyla güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: Matters/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var matter = await _context.Matters.FindAsync(id);
        if (matter == null)
            return NotFound();

        await _auditService.LogAsync("DELETE", "Matter", id, oldValues: matter);

        _context.Matters.Remove(matter);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Dava başarıyla silindi.";
        return RedirectToAction(nameof(Index));
    }
}
