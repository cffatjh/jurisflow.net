using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlowASP.Data;
using JurisFlowASP.Models;
using JurisFlowASP.ViewModels;
using JurisFlowASP.Services;

namespace JurisFlowASP.Controllers;

[Authorize]
public class TasksController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public TasksController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    // GET: Tasks (Kanban Board View)
    public async Task<IActionResult> Index(string? matterId = null)
    {
        var query = _context.Tasks
            .Include(t => t.Matter)
            .Include(t => t.AssignedToUser)
            .AsQueryable();

        if (!string.IsNullOrEmpty(matterId))
            query = query.Where(t => t.MatterId == matterId);

        var tasks = await query.ToListAsync();

        var board = new TaskBoardViewModel
        {
            ToDo = tasks.Where(t => t.Status == "To Do").Select(MapToCard).ToList(),
            InProgress = tasks.Where(t => t.Status == "In Progress").Select(MapToCard).ToList(),
            Review = tasks.Where(t => t.Status == "Review").Select(MapToCard).ToList(),
            Done = tasks.Where(t => t.Status == "Done").Select(MapToCard).ToList()
        };

        ViewBag.Matters = await _context.Matters.Where(m => m.Status != "Closed").OrderBy(m => m.Name).ToListAsync();
        ViewBag.CurrentMatterId = matterId;

        return View(board);
    }

    // GET: Tasks/List (Table View)
    public async Task<IActionResult> List(string? status = null, string? priority = null, string? matterId = null)
    {
        var query = _context.Tasks.Include(t => t.Matter).Include(t => t.AssignedToUser).AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);
        if (!string.IsNullOrEmpty(priority))
            query = query.Where(t => t.Priority == priority);
        if (!string.IsNullOrEmpty(matterId))
            query = query.Where(t => t.MatterId == matterId);

        var tasks = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

        ViewBag.Matters = await _context.Matters.OrderBy(m => m.Name).ToListAsync();
        return View(tasks);
    }

    // GET: Tasks/Create
    public async Task<IActionResult> Create(string? matterId = null)
    {
        ViewBag.Matters = await _context.Matters.Where(m => m.Status != "Closed").OrderBy(m => m.Name).ToListAsync();
        ViewBag.Users = await _context.Users.OrderBy(u => u.Name).ToListAsync();
        
        return View(new TaskCreateViewModel { MatterId = matterId });
    }

    // POST: Tasks/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaskCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Matters = await _context.Matters.Where(m => m.Status != "Closed").OrderBy(m => m.Name).ToListAsync();
            ViewBag.Users = await _context.Users.OrderBy(u => u.Name).ToListAsync();
            return View(model);
        }

        var task = new Models.Task
        {
            Title = model.Title,
            Description = model.Description,
            DueDate = model.DueDate,
            Priority = model.Priority,
            Status = model.Status,
            MatterId = model.MatterId,
            AssignedToId = model.AssignedToId
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("CREATE", "Task", task.Id, newValues: task);

        TempData["Success"] = "Görev başarıyla oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Tasks/Edit/5
    public async Task<IActionResult> Edit(string id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null)
            return NotFound();

        ViewBag.Matters = await _context.Matters.OrderBy(m => m.Name).ToListAsync();
        ViewBag.Users = await _context.Users.OrderBy(u => u.Name).ToListAsync();

        var model = new TaskCreateViewModel
        {
            Title = task.Title,
            Description = task.Description,
            DueDate = task.DueDate,
            Priority = task.Priority,
            Status = task.Status,
            MatterId = task.MatterId,
            AssignedToId = task.AssignedToId
        };

        return View(model);
    }

    // POST: Tasks/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, TaskCreateViewModel model)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.Matters = await _context.Matters.OrderBy(m => m.Name).ToListAsync();
            ViewBag.Users = await _context.Users.OrderBy(u => u.Name).ToListAsync();
            return View(model);
        }

        var oldStatus = task.Status;

        task.Title = model.Title;
        task.Description = model.Description;
        task.DueDate = model.DueDate;
        task.Priority = model.Priority;
        task.Status = model.Status;
        task.MatterId = model.MatterId;
        task.AssignedToId = model.AssignedToId;
        task.UpdatedAt = DateTime.UtcNow;

        if (model.Status == "Done" && oldStatus != "Done")
            task.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("UPDATE", "Task", task.Id, newValues: model);

        TempData["Success"] = "Görev başarıyla güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Tasks/UpdateStatus (AJAX for Kanban drag-drop)
    [HttpPost]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateTaskStatusRequest request)
    {
        var task = await _context.Tasks.FindAsync(request.TaskId);
        if (task == null)
            return NotFound();

        var oldStatus = task.Status;
        task.Status = request.NewStatus;
        task.UpdatedAt = DateTime.UtcNow;

        if (request.NewStatus == "Done" && oldStatus != "Done")
            task.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("UPDATE", "Task", task.Id, 
            oldValues: new { Status = oldStatus }, 
            newValues: new { Status = request.NewStatus });

        return Ok(new { success = true });
    }

    // POST: Tasks/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null)
            return NotFound();

        await _auditService.LogAsync("DELETE", "Task", id, oldValues: task);

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Görev başarıyla silindi.";
        return RedirectToAction(nameof(Index));
    }

    private static TaskCardViewModel MapToCard(Models.Task t) => new()
    {
        Id = t.Id,
        Title = t.Title,
        Description = t.Description,
        DueDate = t.DueDate,
        Priority = t.Priority,
        Status = t.Status,
        MatterName = t.Matter?.Name,
        AssignedTo = t.AssignedToUser?.Name
    };
}

public class UpdateTaskStatusRequest
{
    public string TaskId { get; set; } = "";
    public string NewStatus { get; set; } = "";
}
