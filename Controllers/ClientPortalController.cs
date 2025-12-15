using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlowASP.Data;
using JurisFlowASP.Services;

namespace JurisFlowASP.Controllers;

/// <summary>
/// Client Portal Controller - for client-side access
/// </summary>
public class ClientPortalController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthService _authService;
    private readonly IAuditService _auditService;

    public ClientPortalController(ApplicationDbContext context, IAuthService authService, IAuditService auditService)
    {
        _context = context;
        _authService = authService;
        _auditService = auditService;
    }

    // GET: ClientPortal/Login
    [HttpGet]
    public IActionResult Login()
    {
        if (HttpContext.Session.GetString("ClientId") != null)
            return RedirectToAction(nameof(Dashboard));
        
        return View();
    }

    // POST: ClientPortal/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.Email == email && c.PortalAccess);
        
        if (client == null || string.IsNullOrEmpty(client.PortalPasswordHash))
        {
            ViewBag.Error = "Geçersiz giriş bilgileri veya portal erişimi kapalı.";
            return View();
        }

        if (!_authService.VerifyPassword(password, client.PortalPasswordHash))
        {
            ViewBag.Error = "Geçersiz şifre.";
            return View();
        }

        client.LastLogin = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        HttpContext.Session.SetString("ClientId", client.Id);
        HttpContext.Session.SetString("ClientName", client.Name);
        HttpContext.Session.SetString("ClientEmail", client.Email);

        await _auditService.LogAsync("CLIENT_LOGIN", "Client", client.Id);

        return RedirectToAction(nameof(Dashboard));
    }

    // GET: ClientPortal/Dashboard
    public async Task<IActionResult> Dashboard()
    {
        var clientId = HttpContext.Session.GetString("ClientId");
        if (clientId == null)
            return RedirectToAction(nameof(Login));

        var client = await _context.Clients
            .Include(c => c.Matters)
            .Include(c => c.Invoices)
            .FirstOrDefaultAsync(c => c.Id == clientId);

        if (client == null)
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        ViewBag.ClientName = client.Name;
        ViewBag.ActiveMatters = client.Matters.Count(m => m.Status != "Closed");
        ViewBag.TotalInvoices = client.Invoices.Count;
        ViewBag.PendingInvoices = client.Invoices.Where(i => i.Status == "Sent" || i.Status == "Overdue").Sum(i => i.Amount);

        return View(client);
    }

    // GET: ClientPortal/Matters
    public async Task<IActionResult> Matters()
    {
        var clientId = HttpContext.Session.GetString("ClientId");
        if (clientId == null)
            return RedirectToAction(nameof(Login));

        var matters = await _context.Matters
            .Where(m => m.ClientId == clientId)
            .OrderByDescending(m => m.OpenDate)
            .ToListAsync();

        return View(matters);
    }

    // GET: ClientPortal/MatterDetails/5
    public async Task<IActionResult> MatterDetails(string id)
    {
        var clientId = HttpContext.Session.GetString("ClientId");
        if (clientId == null)
            return RedirectToAction(nameof(Login));

        var matter = await _context.Matters
            .Include(m => m.Documents)
            .Include(m => m.Events)
            .FirstOrDefaultAsync(m => m.Id == id && m.ClientId == clientId);

        if (matter == null)
            return NotFound();

        return View(matter);
    }

    // GET: ClientPortal/Documents
    public async Task<IActionResult> Documents()
    {
        var clientId = HttpContext.Session.GetString("ClientId");
        if (clientId == null)
            return RedirectToAction(nameof(Login));

        var documents = await _context.Documents
            .Include(d => d.Matter)
            .Where(d => d.Matter != null && d.Matter.ClientId == clientId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return View(documents);
    }

    // GET: ClientPortal/Invoices
    public async Task<IActionResult> Invoices()
    {
        var clientId = HttpContext.Session.GetString("ClientId");
        if (clientId == null)
            return RedirectToAction(nameof(Login));

        var invoices = await _context.Invoices
            .Where(i => i.ClientId == clientId)
            .OrderByDescending(i => i.DueDate)
            .ToListAsync();

        return View(invoices);
    }

    // GET: ClientPortal/Messages
    public async Task<IActionResult> Messages()
    {
        var clientId = HttpContext.Session.GetString("ClientId");
        if (clientId == null)
            return RedirectToAction(nameof(Login));

        var messages = await _context.ClientMessages
            .Include(m => m.Matter)
            .Where(m => m.ClientId == clientId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        var matters = await _context.Matters
            .Where(m => m.ClientId == clientId)
            .ToListAsync();

        ViewBag.Matters = matters;
        return View(messages);
    }

    // POST: ClientPortal/SendMessage
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(string subject, string message, string? matterId)
    {
        var clientId = HttpContext.Session.GetString("ClientId");
        if (clientId == null)
            return RedirectToAction(nameof(Login));

        var msg = new Models.ClientMessage
        {
            ClientId = clientId,
            Subject = subject,
            Message = message,
            MatterId = matterId
        };

        _context.ClientMessages.Add(msg);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("SEND_MESSAGE", "ClientMessage", msg.Id);

        TempData["Success"] = "Mesajınız gönderildi.";
        return RedirectToAction(nameof(Messages));
    }

    // POST: ClientPortal/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var clientId = HttpContext.Session.GetString("ClientId");
        if (clientId != null)
            await _auditService.LogAsync("CLIENT_LOGOUT", "Client", clientId);

        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }
}
