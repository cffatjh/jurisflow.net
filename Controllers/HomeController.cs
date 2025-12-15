using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlowASP.Data;
using JurisFlowASP.Models;
using JurisFlowASP.ViewModels;

namespace JurisFlowASP.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        var dashboard = new DashboardViewModel
        {
            TotalClients = await _context.Clients.CountAsync(),
            ActiveMatters = await _context.Matters.CountAsync(m => m.Status != "Closed"),
            PendingTasks = await _context.Tasks.CountAsync(t => t.Status != "Done"),
            TotalBilled = await _context.TimeEntries.Where(t => t.IsBilled).SumAsync(t => (decimal)(t.Duration / 60.0m) * t.Rate),
            TotalUnbilled = await _context.TimeEntries.Where(t => !t.IsBilled).SumAsync(t => (decimal)(t.Duration / 60.0m) * t.Rate),
            OverdueInvoices = await _context.Invoices.Where(i => i.Status == "Overdue").SumAsync(i => i.Amount),

            // Upcoming events (next 7 days)
            UpcomingEvents = await _context.CalendarEvents
                .Where(e => e.Date >= now && e.Date <= now.AddDays(7))
                .OrderBy(e => e.Date)
                .Take(5)
                .Select(e => new CalendarEventViewModel
                {
                    Id = e.Id,
                    Title = e.Title,
                    Date = e.Date,
                    Type = e.Type,
                    MatterName = e.Matter != null ? e.Matter.Name : null
                })
                .ToListAsync(),

            // Recent tasks
            RecentTasks = await _context.Tasks
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .Select(t => new TaskCardViewModel
                {
                    Id = t.Id,
                    Title = t.Title,
                    DueDate = t.DueDate,
                    Priority = t.Priority,
                    Status = t.Status,
                    MatterName = t.Matter != null ? t.Matter.Name : null
                })
                .ToListAsync(),

            // Recent matters
            RecentMatters = await _context.Matters
                .Include(m => m.Client)
                .OrderByDescending(m => m.OpenDate)
                .Take(5)
                .Select(m => new MatterListViewModel
                {
                    Id = m.Id,
                    CaseNumber = m.CaseNumber,
                    Name = m.Name,
                    ClientName = m.Client != null ? m.Client.Name : "",
                    PracticeArea = m.PracticeArea,
                    Status = m.Status,
                    OpenDate = m.OpenDate
                })
                .ToListAsync(),

            // Matters by status for chart
            MattersByStatus = await _context.Matters
                .GroupBy(m => m.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count)
        };

        return View(dashboard);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
