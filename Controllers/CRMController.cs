using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlowASP.Data;
using JurisFlowASP.Models;
using JurisFlowASP.ViewModels;
using JurisFlowASP.Services;

namespace JurisFlowASP.Controllers;

[Authorize]
public class CRMController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public CRMController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    // GET: CRM (Leads)
    public async Task<IActionResult> Index(string? status = null, string? practiceArea = null)
    {
        var query = _context.Leads.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(l => l.Status == status);

        if (!string.IsNullOrEmpty(practiceArea))
            query = query.Where(l => l.PracticeArea == practiceArea);

        var leads = await query.OrderByDescending(l => l.EstimatedValue).ToListAsync();

        ViewBag.CurrentStatus = status;
        ViewBag.CurrentPracticeArea = practiceArea;

        // Stats
        ViewBag.TotalLeads = await _context.Leads.CountAsync();
        ViewBag.NewLeads = await _context.Leads.CountAsync(l => l.Status == "New");
        ViewBag.TotalValue = await _context.Leads.SumAsync(l => l.EstimatedValue);
        ViewBag.ConvertedValue = await _context.Leads.Where(l => l.Status == "Converted").SumAsync(l => l.EstimatedValue);

        return View(leads);
    }

    // GET: CRM/Create
    public IActionResult Create()
    {
        return View(new LeadCreateViewModel());
    }

    // POST: CRM/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeadCreateViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var lead = new Lead
        {
            Name = model.Name,
            Source = model.Source,
            Status = model.Status,
            EstimatedValue = model.EstimatedValue,
            PracticeArea = model.PracticeArea
        };

        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("CREATE", "Lead", lead.Id, newValues: lead);

        TempData["Success"] = "Potansiyel müşteri eklendi.";
        return RedirectToAction(nameof(Index));
    }

    // GET: CRM/Edit/5
    public async Task<IActionResult> Edit(string id)
    {
        var lead = await _context.Leads.FindAsync(id);
        if (lead == null)
            return NotFound();

        var model = new LeadCreateViewModel
        {
            Name = lead.Name,
            Source = lead.Source,
            Status = lead.Status,
            EstimatedValue = lead.EstimatedValue ?? 0,
            PracticeArea = lead.PracticeArea
        };

        return View(model);
    }

    // POST: CRM/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, LeadCreateViewModel model)
    {
        var lead = await _context.Leads.FindAsync(id);
        if (lead == null)
            return NotFound();

        if (!ModelState.IsValid)
            return View(model);

        lead.Name = model.Name;
        lead.Source = model.Source;
        lead.Status = model.Status;
        lead.EstimatedValue = model.EstimatedValue;
        lead.PracticeArea = model.PracticeArea;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("UPDATE", "Lead", lead.Id, newValues: model);

        TempData["Success"] = "Potansiyel müşteri güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    // POST: CRM/Convert/5 (Convert lead to client)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(string id)
    {
        var lead = await _context.Leads.FindAsync(id);
        if (lead == null)
            return NotFound();

        // Create new client from lead
        var client = new Client
        {
            Name = lead.Name,
            Email = $"{lead.Name.ToLower().Replace(" ", ".")}@example.com",
            Type = "Individual",
            Status = "Active",
            Notes = $"Lead'den dönüştürüldü. Kaynak: {lead.Source}, Tahmini Değer: {lead.EstimatedValue:C}"
        };

        _context.Clients.Add(client);
        
        lead.Status = "Converted";
        
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("CONVERT", "Lead", lead.Id, 
            details: $"Converted to client {client.Id}");

        TempData["Success"] = "Lead müvekkile dönüştürüldü.";
        return RedirectToAction("Edit", "Clients", new { id = client.Id });
    }

    // POST: CRM/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var lead = await _context.Leads.FindAsync(id);
        if (lead == null)
            return NotFound();

        await _auditService.LogAsync("DELETE", "Lead", id, oldValues: lead);

        _context.Leads.Remove(lead);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Potansiyel müşteri silindi.";
        return RedirectToAction(nameof(Index));
    }
}
