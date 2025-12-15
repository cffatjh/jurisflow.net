using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlowASP.Data;
using JurisFlowASP.Models;
using JurisFlowASP.ViewModels;
using JurisFlowASP.Services;

namespace JurisFlowASP.Controllers;

[Authorize]
public class DocumentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IWebHostEnvironment _environment;

    public DocumentsController(ApplicationDbContext context, IAuditService auditService, IWebHostEnvironment environment)
    {
        _context = context;
        _auditService = auditService;
        _environment = environment;
    }

    // GET: Documents
    public async Task<IActionResult> Index(string? matterId = null, string? search = null)
    {
        var query = _context.Documents.Include(d => d.Matter).AsQueryable();

        if (!string.IsNullOrEmpty(matterId))
            query = query.Where(d => d.MatterId == matterId);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(d => d.Name.Contains(search) || d.FileName.Contains(search));

        var documents = await query
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DocumentListViewModel
            {
                Id = d.Id,
                Name = d.Name,
                FileName = d.FileName,
                FileSize = d.FileSize,
                MimeType = d.MimeType,
                MatterName = d.Matter != null ? d.Matter.Name : null,
                CreatedAt = d.CreatedAt
            })
            .ToListAsync();

        ViewBag.Matters = await _context.Matters.OrderBy(m => m.Name).ToListAsync();
        ViewBag.CurrentMatterId = matterId;
        ViewBag.Search = search;

        return View(documents);
    }

    // GET: Documents/Upload
    public async Task<IActionResult> Upload(string? matterId = null)
    {
        ViewBag.Matters = await _context.Matters.OrderBy(m => m.Name).ToListAsync();
        return View(new DocumentUploadViewModel { MatterId = matterId });
    }

    // POST: Documents/Upload
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(DocumentUploadViewModel model)
    {
        if (model.File == null || model.File.Length == 0)
        {
            ModelState.AddModelError("File", "Lütfen bir dosya seçin.");
            ViewBag.Matters = await _context.Matters.OrderBy(m => m.Name).ToListAsync();
            return View(model);
        }

        // Create uploads directory if not exists
        var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
        if (!Directory.Exists(uploadsPath))
            Directory.CreateDirectory(uploadsPath);

        // Generate unique filename
        var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(model.File.FileName)}";
        var filePath = Path.Combine(uploadsPath, uniqueFileName);

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await model.File.CopyToAsync(stream);
        }

        var document = new Document
        {
            Name = Path.GetFileNameWithoutExtension(model.File.FileName),
            FileName = model.File.FileName,
            FilePath = $"/uploads/{uniqueFileName}",
            FileSize = (int)model.File.Length,
            MimeType = model.File.ContentType,
            MatterId = model.MatterId,
            Description = model.Description,
            Tags = model.Tags,
            UploadedBy = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("UPLOAD", "Document", document.Id, newValues: new { document.Name, document.FileName });

        TempData["Success"] = "Dosya başarıyla yüklendi.";
        return RedirectToAction(nameof(Index), new { matterId = model.MatterId });
    }

    // GET: Documents/Download/5
    public async Task<IActionResult> Download(string id)
    {
        var document = await _context.Documents.FindAsync(id);
        if (document == null)
            return NotFound();

        var filePath = Path.Combine(_environment.WebRootPath, document.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(filePath))
            return NotFound("Dosya bulunamadı.");

        await _auditService.LogAsync("DOWNLOAD", "Document", id, details: document.FileName);

        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
        return File(fileBytes, document.MimeType, document.FileName);
    }

    // GET: Documents/Preview/5
    public async Task<IActionResult> Preview(string id)
    {
        var document = await _context.Documents
            .Include(d => d.Matter)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null)
            return NotFound();

        await _auditService.LogAsync("VIEW", "Document", id);
        return View(document);
    }

    // POST: Documents/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var document = await _context.Documents.FindAsync(id);
        if (document == null)
            return NotFound();

        // Delete physical file
        var filePath = Path.Combine(_environment.WebRootPath, document.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);

        await _auditService.LogAsync("DELETE", "Document", id, oldValues: new { document.Name, document.FileName });

        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Dosya başarıyla silindi.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Documents/Update/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string id, string name, string? description, string? tags)
    {
        var document = await _context.Documents.FindAsync(id);
        if (document == null)
            return NotFound();

        document.Name = name;
        document.Description = description;
        document.Tags = tags;
        document.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("UPDATE", "Document", id);

        TempData["Success"] = "Dosya bilgileri güncellendi.";
        return RedirectToAction(nameof(Preview), new { id });
    }
}
