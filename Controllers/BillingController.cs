using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlowASP.Data;
using JurisFlowASP.Models;
using JurisFlowASP.ViewModels;
using JurisFlowASP.Services;

namespace JurisFlowASP.Controllers;

[Authorize]
public class BillingController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IPdfService _pdfService;

    public BillingController(ApplicationDbContext context, IAuditService auditService, IPdfService pdfService)
    {
        _context = context;
        _auditService = auditService;
        _pdfService = pdfService;
    }

    // GET: Billing (Invoices list)
    public async Task<IActionResult> Index(string? status = null, string? clientId = null)
    {
        var query = _context.Invoices.Include(i => i.Client).AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(i => i.Status == status);

        if (!string.IsNullOrEmpty(clientId))
            query = query.Where(i => i.ClientId == clientId);

        var invoices = await query.OrderByDescending(i => i.DueDate).ToListAsync();

        ViewBag.Clients = await _context.Clients.OrderBy(c => c.Name).ToListAsync();
        ViewBag.CurrentStatus = status;
        ViewBag.CurrentClientId = clientId;

        // Summary stats
        ViewBag.TotalDraft = await _context.Invoices.Where(i => i.Status == "Draft").SumAsync(i => i.Amount);
        ViewBag.TotalSent = await _context.Invoices.Where(i => i.Status == "Sent").SumAsync(i => i.Amount);
        ViewBag.TotalPaid = await _context.Invoices.Where(i => i.Status == "Paid").SumAsync(i => i.Amount);
        ViewBag.TotalOverdue = await _context.Invoices.Where(i => i.Status == "Overdue").SumAsync(i => i.Amount);

        return View(invoices);
    }

    // GET: Billing/TimeEntries (Unbilled time entries)
    public async Task<IActionResult> TimeEntries(string? matterId = null)
    {
        var query = _context.TimeEntries
            .Include(t => t.Matter)
            .Where(t => !t.IsBilled)
            .AsQueryable();

        if (!string.IsNullOrEmpty(matterId))
            query = query.Where(t => t.MatterId == matterId);

        var entries = await query.OrderByDescending(t => t.Date).ToListAsync();

        ViewBag.Matters = await _context.Matters.OrderBy(m => m.Name).ToListAsync();
        ViewBag.CurrentMatterId = matterId;

        return View(entries);
    }

    // GET: Billing/Create
    public async Task<IActionResult> Create(string? clientId = null)
    {
        ViewBag.Clients = await _context.Clients.Where(c => c.Status == "Active").OrderBy(c => c.Name).ToListAsync();
        ViewBag.Matters = await _context.Matters.OrderBy(m => m.Name).ToListAsync();

        // Get last invoice number
        var lastInvoice = await _context.Invoices.OrderByDescending(i => i.Number).FirstOrDefaultAsync();
        var nextNumber = "INV-0001";
        if (lastInvoice != null && lastInvoice.Number.StartsWith("INV-"))
        {
            if (int.TryParse(lastInvoice.Number[4..], out int num))
                nextNumber = $"INV-{num + 1:D4}";
        }

        ViewBag.NextInvoiceNumber = nextNumber;
        ViewBag.DefaultClientId = clientId;

        return View(new InvoiceCreateViewModel { ClientId = clientId ?? "" });
    }

    // POST: Billing/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string clientId, DateTime dueDate, decimal amount, string? notes)
    {
        if (string.IsNullOrEmpty(clientId))
        {
            TempData["Error"] = "Müvekkil seçimi gerekli.";
            return RedirectToAction(nameof(Create));
        }

        // Generate invoice number
        var lastInvoice = await _context.Invoices.OrderByDescending(i => i.Number).FirstOrDefaultAsync();
        var nextNumber = "INV-0001";
        if (lastInvoice != null && lastInvoice.Number.StartsWith("INV-"))
        {
            if (int.TryParse(lastInvoice.Number[4..], out int num))
                nextNumber = $"INV-{num + 1:D4}";
        }

        var invoice = new Invoice
        {
            Number = nextNumber,
            ClientId = clientId,
            Amount = amount,
            DueDate = dueDate,
            Status = "Draft"
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("CREATE", "Invoice", invoice.Id, newValues: invoice);

        TempData["Success"] = "Fatura oluşturuldu.";
        return RedirectToAction(nameof(Details), new { id = invoice.Id });
    }

    // GET: Billing/Details/5
    public async Task<IActionResult> Details(string id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Client)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
            return NotFound();

        return View(invoice);
    }

    // POST: Billing/UpdateStatus/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(string id, string status)
    {
        var invoice = await _context.Invoices.FindAsync(id);
        if (invoice == null)
            return NotFound();

        var oldStatus = invoice.Status;
        invoice.Status = status;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("UPDATE", "Invoice", id, 
            oldValues: new { Status = oldStatus }, 
            newValues: new { Status = status });

        TempData["Success"] = "Fatura durumu güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // GET: Billing/Print/5
    public async Task<IActionResult> Print(string id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Client)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
            return NotFound();

        var pdfData = new InvoicePdfData
        {
            InvoiceNumber = invoice.Number,
            Date = DateTime.Now,
            DueDate = invoice.DueDate,
            ClientName = invoice.Client?.Name ?? "",
            ClientAddress = invoice.Client?.Address ?? "",
            ClientEmail = invoice.Client?.Email ?? "",
            LineItems = new List<InvoicePdfLineItem>
            {
                new InvoicePdfLineItem
                {
                    Description = "Hukuki Danışmanlık Hizmeti",
                    Quantity = 1,
                    UnitPrice = invoice.Amount,
                    Total = invoice.Amount
                }
            },
            Subtotal = invoice.Amount,
            VatRate = 18,
            VatAmount = invoice.Amount * 0.18m,
            Total = invoice.Amount * 1.18m
        };

        var pdf = _pdfService.GenerateInvoicePdf(pdfData);
        
        await _auditService.LogAsync("PRINT", "Invoice", id);

        return File(pdf, "text/html", $"Fatura-{invoice.Number}.html");
    }

    // POST: Billing/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var invoice = await _context.Invoices.FindAsync(id);
        if (invoice == null)
            return NotFound();

        await _auditService.LogAsync("DELETE", "Invoice", id, oldValues: invoice);

        _context.Invoices.Remove(invoice);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Fatura silindi.";
        return RedirectToAction(nameof(Index));
    }
}
