using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlowASP.Data;
using JurisFlowASP.Models;
using JurisFlowASP.ViewModels;
using JurisFlowASP.Services;

namespace JurisFlowASP.Controllers;

[Authorize]
public class TimeTrackerController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public TimeTrackerController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    // GET: TimeTracker
    public async Task<IActionResult> Index(string? matterId = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.TimeEntries
            .Include(t => t.Matter)
            .AsQueryable();

        if (!string.IsNullOrEmpty(matterId))
            query = query.Where(t => t.MatterId == matterId);

        if (startDate.HasValue)
            query = query.Where(t => t.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.Date <= endDate.Value);

        var entries = await query.OrderByDescending(t => t.Date).ToListAsync();

        // Expenses
        var expenseQuery = _context.Expenses.Include(e => e.Matter).AsQueryable();
        if (!string.IsNullOrEmpty(matterId))
            expenseQuery = expenseQuery.Where(e => e.MatterId == matterId);

        var expenses = await expenseQuery.OrderByDescending(e => e.Date).ToListAsync();

        ViewBag.Matters = await _context.Matters.Where(m => m.Status != "Closed").OrderBy(m => m.Name).ToListAsync();
        ViewBag.CurrentMatterId = matterId;
        ViewBag.StartDate = startDate;
        ViewBag.EndDate = endDate;
        ViewBag.Expenses = expenses;

        // Stats
        ViewBag.TotalHours = entries.Sum(e => e.Duration) / 60.0m;
        ViewBag.TotalBillable = entries.Sum(e => (e.Duration / 60.0m) * e.Rate);
        ViewBag.TotalExpenses = expenses.Sum(e => e.Amount);

        return View(entries);
    }

    // GET: TimeTracker/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.Matters = await _context.Matters.Where(m => m.Status != "Closed").OrderBy(m => m.Name).ToListAsync();
        return View(new TimeEntryCreateViewModel { Date = DateTime.Now });
    }

    // POST: TimeTracker/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TimeEntryCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Matters = await _context.Matters.Where(m => m.Status != "Closed").OrderBy(m => m.Name).ToListAsync();
            return View(model);
        }

        // Get matter's billable rate if not specified
        if (model.Rate == 0 && !string.IsNullOrEmpty(model.MatterId))
        {
            var matter = await _context.Matters.FindAsync(model.MatterId);
            if (matter != null)
                model.Rate = matter.BillableRate;
        }

        var entry = new TimeEntry
        {
            MatterId = model.MatterId,
            Description = model.Description,
            Duration = model.Duration,
            Rate = model.Rate,
            Date = model.Date,
            Type = "time"
        };

        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("CREATE", "TimeEntry", entry.Id, newValues: entry);

        TempData["Success"] = "Zaman kaydı oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    // POST: TimeTracker/MarkAsBilled
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsBilled(string[] ids)
    {
        if (ids == null || ids.Length == 0)
        {
            TempData["Error"] = "Seçili kayıt yok.";
            return RedirectToAction(nameof(Index));
        }

        var entries = await _context.TimeEntries.Where(e => ids.Contains(e.Id)).ToListAsync();
        foreach (var entry in entries)
        {
            entry.IsBilled = true;
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("UPDATE", "TimeEntry", null, details: $"Marked {ids.Length} entries as billed");

        TempData["Success"] = $"{ids.Length} kayıt faturalandı olarak işaretlendi.";
        return RedirectToAction(nameof(Index));
    }

    // POST: TimeTracker/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var entry = await _context.TimeEntries.FindAsync(id);
        if (entry == null)
            return NotFound();

        await _auditService.LogAsync("DELETE", "TimeEntry", id, oldValues: entry);

        _context.TimeEntries.Remove(entry);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Kayıt silindi.";
        return RedirectToAction(nameof(Index));
    }

    // GET: TimeTracker/CreateExpense
    public async Task<IActionResult> CreateExpense()
    {
        ViewBag.Matters = await _context.Matters.Where(m => m.Status != "Closed").OrderBy(m => m.Name).ToListAsync();
        return View();
    }

    // POST: TimeTracker/CreateExpense
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateExpense(string? matterId, string description, decimal amount, string category, DateTime date)
    {
        var expense = new Expense
        {
            MatterId = matterId,
            Description = description,
            Amount = amount,
            Category = category,
            Date = date,
            Type = "expense"
        };

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("CREATE", "Expense", expense.Id, newValues: expense);

        TempData["Success"] = "Gider kaydı oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }
}
