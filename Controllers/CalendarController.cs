using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlowASP.Data;
using JurisFlowASP.Models;
using JurisFlowASP.ViewModels;
using JurisFlowASP.Services;

namespace JurisFlowASP.Controllers;

[Authorize]
public class CalendarController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public CalendarController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    // GET: Calendar
    public async Task<IActionResult> Index(int? month = null, int? year = null)
    {
        var now = DateTime.Now;
        month ??= now.Month;
        year ??= now.Year;

        var startDate = new DateTime(year.Value, month.Value, 1);
        var endDate = startDate.AddMonths(1);

        var events = await _context.CalendarEvents
            .Include(e => e.Matter)
            .Where(e => e.Date >= startDate && e.Date < endDate)
            .OrderBy(e => e.Date)
            .Select(e => new CalendarEventViewModel
            {
                Id = e.Id,
                Title = e.Title,
                Date = e.Date,
                Type = e.Type,
                MatterName = e.Matter != null ? e.Matter.Name : null
            })
            .ToListAsync();

        // Include task deadlines
        var taskDeadlines = await _context.Tasks
            .Include(t => t.Matter)
            .Where(t => t.DueDate.HasValue && t.DueDate >= startDate && t.DueDate < endDate && t.Status != "Done")
            .Select(t => new CalendarEventViewModel
            {
                Id = "task-" + t.Id,
                Title = "📋 " + t.Title,
                Date = t.DueDate!.Value,
                Type = "Deadline",
                MatterName = t.Matter != null ? t.Matter.Name : null
            })
            .ToListAsync();

        events.AddRange(taskDeadlines);
        events = events.OrderBy(e => e.Date).ToList();

        ViewBag.Month = month.Value;
        ViewBag.Year = year.Value;
        ViewBag.MonthName = new DateTime(year.Value, month.Value, 1).ToString("MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));

        return View(events);
    }

    // GET: Calendar/Create
    public async Task<IActionResult> Create(DateTime? date = null)
    {
        ViewBag.Matters = await _context.Matters.Where(m => m.Status != "Closed").OrderBy(m => m.Name).ToListAsync();
        ViewBag.DefaultDate = date ?? DateTime.Now;
        return View();
    }

    // POST: Calendar/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CalendarEvent model)
    {
        if (string.IsNullOrEmpty(model.Title))
        {
            ModelState.AddModelError("Title", "Başlık gerekli");
            ViewBag.Matters = await _context.Matters.Where(m => m.Status != "Closed").OrderBy(m => m.Name).ToListAsync();
            return View(model);
        }

        _context.CalendarEvents.Add(model);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("CREATE", "CalendarEvent", model.Id, newValues: model);

        TempData["Success"] = "Etkinlik başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Index), new { month = model.Date.Month, year = model.Date.Year });
    }

    // POST: Calendar/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var ev = await _context.CalendarEvents.FindAsync(id);
        if (ev == null)
            return NotFound();

        var month = ev.Date.Month;
        var year = ev.Date.Year;

        await _auditService.LogAsync("DELETE", "CalendarEvent", id, oldValues: ev);

        _context.CalendarEvents.Remove(ev);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Etkinlik silindi.";
        return RedirectToAction(nameof(Index), new { month, year });
    }

    // GET: Calendar/GetEvents (AJAX for calendar widget)
    [HttpGet]
    public async Task<IActionResult> GetEvents(DateTime start, DateTime end)
    {
        var events = await _context.CalendarEvents
            .Where(e => e.Date >= start && e.Date <= end)
            .Select(e => new
            {
                id = e.Id,
                title = e.Title,
                start = e.Date,
                type = e.Type,
                color = e.Type == "Court" ? "#ef4444" : e.Type == "Meeting" ? "#3b82f6" : "#f59e0b"
            })
            .ToListAsync();

        return Json(events);
    }
}
