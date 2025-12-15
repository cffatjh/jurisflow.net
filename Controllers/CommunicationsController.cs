using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlowASP.Data;
using JurisFlowASP.Models;
using JurisFlowASP.Services;
using System.Security.Claims;

namespace JurisFlowASP.Controllers;

[Authorize]
public class CommunicationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IAuditService _auditService;

    public CommunicationsController(ApplicationDbContext context, IEmailService emailService, IAuditService auditService)
    {
        _context = context;
        _emailService = emailService;
        _auditService = auditService;
    }

    // GET: Communications
    public async Task<IActionResult> Index(string? tab = "inbox")
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Get client messages (inbox)
        var messages = await _context.ClientMessages
            .Include(m => m.Client)
            .Include(m => m.Matter)
            .OrderByDescending(m => m.CreatedAt)
            .Take(50)
            .ToListAsync();

        // Get recent notifications
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(20)
            .ToListAsync();

        ViewBag.Messages = messages;
        ViewBag.Notifications = notifications;
        ViewBag.CurrentTab = tab;
        ViewBag.UnreadCount = messages.Count(m => !m.Read);

        // Get clients and matters for compose
        ViewBag.Clients = await _context.Clients.OrderBy(c => c.Name).ToListAsync();
        ViewBag.Matters = await _context.Matters.OrderBy(m => m.Name).ToListAsync();

        return View();
    }

    // POST: Communications/SendEmail
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendEmail(string toEmail, string subject, string body, string? matterId)
    {
        if (string.IsNullOrEmpty(toEmail) || string.IsNullOrEmpty(subject))
        {
            TempData["Error"] = "E-posta ve konu alanları zorunludur.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _emailService.SendEmailAsync(toEmail, subject, body);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            await _auditService.LogAsync("SEND_EMAIL", "Email", null, 
                details: $"To: {toEmail}, Subject: {subject}");

            TempData["Success"] = $"E-posta başarıyla gönderildi: {toEmail}";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"E-posta gönderilemedi: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: Communications/MarkAsRead/5
    [HttpPost]
    public async Task<IActionResult> MarkAsRead(string id)
    {
        var message = await _context.ClientMessages.FindAsync(id);
        if (message != null)
        {
            message.Read = true;
            await _context.SaveChangesAsync();
        }
        return Ok();
    }

    // POST: Communications/Reply
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(string messageId, string replyText)
    {
        var originalMessage = await _context.ClientMessages
            .Include(m => m.Client)
            .FirstOrDefaultAsync(m => m.Id == messageId);

        if (originalMessage == null || originalMessage.Client == null)
        {
            TempData["Error"] = "Mesaj bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        // Send email reply
        var subject = $"RE: {originalMessage.Subject}";
        await _emailService.SendEmailAsync(originalMessage.Client.Email, subject, replyText);

        // Mark original as read
        originalMessage.Read = true;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("REPLY_MESSAGE", "ClientMessage", messageId,
            details: $"Reply to {originalMessage.Client.Email}");

        TempData["Success"] = "Yanıt gönderildi.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Communications/CreateNotification
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateNotification(string? userId, string? clientId, string title, string message, string type, string? link)
    {
        var notification = new Notification
        {
            UserId = userId,
            ClientId = clientId,
            Title = title,
            Message = message,
            Type = type,
            Link = link
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("CREATE", "Notification", notification.Id);

        TempData["Success"] = "Bildirim oluşturuldu.";
        return RedirectToAction(nameof(Index), new { tab = "notifications" });
    }

    // DELETE: Communications/DeleteMessage/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMessage(string id)
    {
        var message = await _context.ClientMessages.FindAsync(id);
        if (message != null)
        {
            _context.ClientMessages.Remove(message);
            await _context.SaveChangesAsync();
            await _auditService.LogAsync("DELETE", "ClientMessage", id);
        }

        TempData["Success"] = "Mesaj silindi.";
        return RedirectToAction(nameof(Index));
    }
}
