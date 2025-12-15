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
public class SettingsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthService _authService;
    private readonly IAuditService _auditService;

    public SettingsController(ApplicationDbContext context, IAuthService authService, IAuditService auditService)
    {
        _context = context;
        _authService = authService;
        _auditService = auditService;
    }

    // GET: Settings
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var user = await _context.Users.FindAsync(userId);
        
        if (user == null)
            return RedirectToAction("Login", "Auth");

        var model = new UserProfileViewModel
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            Mobile = user.Mobile,
            Address = user.Address,
            City = user.City,
            BarNumber = user.BarNumber,
            Bio = user.Bio
        };

        return View(model);
    }

    // POST: Settings/UpdateProfile
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(UserProfileViewModel model)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var user = await _context.Users.FindAsync(userId);
        
        if (user == null)
            return NotFound();

        if (!ModelState.IsValid)
            return View("Index", model);

        user.Name = model.Name;
        user.Phone = model.Phone;
        user.Mobile = model.Mobile;
        user.Address = model.Address;
        user.City = model.City;
        user.BarNumber = model.BarNumber;
        user.Bio = model.Bio;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("UPDATE", "User", user.Id, newValues: model);

        TempData["Success"] = "Profil başarıyla güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Settings/ChangePassword
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    // POST: Settings/ChangePassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return RedirectToAction("Login", "Auth");

        var result = await _authService.ChangePasswordAsync(userId, model.CurrentPassword, model.NewPassword);
        if (!result)
        {
            ModelState.AddModelError("CurrentPassword", "Mevcut şifre yanlış.");
            return View(model);
        }

        await _auditService.LogAsync("CHANGE_PASSWORD", "User", userId);

        TempData["Success"] = "Şifre başarıyla değiştirildi.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Settings/Notifications
    public async Task<IActionResult> Notifications()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();

        return View(notifications);
    }

    // POST: Settings/MarkNotificationRead/5
    [HttpPost]
    public async Task<IActionResult> MarkNotificationRead(string id)
    {
        var notification = await _context.Notifications.FindAsync(id);
        if (notification != null)
        {
            notification.Read = true;
            await _context.SaveChangesAsync();
        }

        return Ok();
    }

    // POST: Settings/MarkAllNotificationsRead
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllNotificationsRead()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.Read)
            .ToListAsync();

        foreach (var n in notifications)
            n.Read = true;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Tüm bildirimler okundu olarak işaretlendi.";
        return RedirectToAction(nameof(Notifications));
    }

    // GET: Settings/AuditLogs (Admin only)
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AuditLogs(int page = 1, string? action = null, string? entityType = null)
    {
        const int pageSize = 50;

        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrEmpty(action))
            query = query.Where(a => a.Action == action);

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(a => a.EntityType == entityType);

        var total = await query.CountAsync();
        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        ViewBag.CurrentAction = action;
        ViewBag.CurrentEntityType = entityType;

        return View(logs);
    }

    // GET: Settings/Users (Admin only)
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Users()
    {
        var users = await _context.Users.OrderBy(u => u.Name).ToListAsync();
        return View(users);
    }

    // POST: Settings/DeleteUser/5 (Admin only)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (id == userId)
        {
            TempData["Error"] = "Kendinizi silemezsiniz.";
            return RedirectToAction(nameof(Users));
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        await _auditService.LogAsync("DELETE", "User", id, oldValues: new { user.Name, user.Email });

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Kullanıcı silindi.";
        return RedirectToAction(nameof(Users));
    }
}
