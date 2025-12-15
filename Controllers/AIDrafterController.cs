using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JurisFlowASP.Data;
using JurisFlowASP.Models;
using JurisFlowASP.Services;
using System.Text;
using System.Text.Json;

namespace JurisFlowASP.Controllers;

[Authorize]
public class AIDrafterController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IAuditService _auditService;
    private readonly IHttpClientFactory _httpClientFactory;

    public AIDrafterController(
        ApplicationDbContext context, 
        IConfiguration configuration, 
        IAuditService auditService,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _configuration = configuration;
        _auditService = auditService;
        _httpClientFactory = httpClientFactory;
    }

    // GET: AIDrafter
    public async Task<IActionResult> Index()
    {
        var matters = await _context.Matters
            .Include(m => m.Client)
            .OrderByDescending(m => m.OpenDate)
            .ToListAsync();

        var templates = await _context.DocumentTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Name)
            .ToListAsync();

        ViewBag.Matters = matters;
        ViewBag.Templates = templates;

        return View();
    }

    // POST: AIDrafter/Generate
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(string prompt, string? matterId, string? templateId, string documentType)
    {
        try
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return Json(new { success = false, error = "Gemini API anahtarı yapılandırılmamış. appsettings.json dosyasına 'Gemini:ApiKey' ekleyin." });
            }

            // Build context
            var contextBuilder = new StringBuilder();
            contextBuilder.AppendLine("Sen bir hukuk asistanısın. Türkiye hukuk sistemine göre profesyonel belgeler hazırlıyorsun.");
            contextBuilder.AppendLine($"Belge türü: {documentType}");

            if (!string.IsNullOrEmpty(matterId))
            {
                var matter = await _context.Matters
                    .Include(m => m.Client)
                    .FirstOrDefaultAsync(m => m.Id == matterId);

                if (matter != null)
                {
                    contextBuilder.AppendLine($"\nDava Bilgileri:");
                    contextBuilder.AppendLine($"- Dosya No: {matter.CaseNumber}");
                    contextBuilder.AppendLine($"- Dava Adı: {matter.Name}");
                    contextBuilder.AppendLine($"- Uzmanlık Alanı: {matter.PracticeArea}");
                    contextBuilder.AppendLine($"- Müvekkil: {matter.Client?.Name}");
                    contextBuilder.AppendLine($"- Açıklama: {matter.Description ?? "Belirtilmemiş"}");
                }
            }

            if (!string.IsNullOrEmpty(templateId))
            {
                var template = await _context.DocumentTemplates.FindAsync(templateId);
                if (template != null)
                {
                    contextBuilder.AppendLine($"\nŞablon: {template.Name}");
                    contextBuilder.AppendLine($"Şablon İçeriği:\n{template.Content}");
                }
            }

            contextBuilder.AppendLine($"\nKullanıcı İsteği: {prompt}");
            contextBuilder.AppendLine("\nLütfen profesyonel, resmi ve hukuki terminolojiye uygun bir belge oluştur.");

            // Call Gemini API
            var client = _httpClientFactory.CreateClient();
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = contextBuilder.ToString() }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 4096
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}",
                content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, error = $"API hatası: {response.StatusCode}" });
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseContent);
            
            var generatedText = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            await _auditService.LogAsync("AI_GENERATE", "AIDrafter", null, 
                details: $"Type: {documentType}, Prompt length: {prompt.Length}");

            return Json(new { success = true, content = generatedText });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // POST: AIDrafter/SaveDocument
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDocument(string name, string content, string? matterId)
    {
        try
        {
            // Save as a document
            var fileName = $"{name.Replace(" ", "_")}_{DateTime.Now:yyyyMMddHHmmss}.txt";
            var filePath = Path.Combine("wwwroot", "uploads", "ai-generated", fileName);
            
            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            
            await System.IO.File.WriteAllTextAsync(filePath, content);

            var document = new Document
            {
                Name = name,
                FileName = fileName,
                FilePath = $"/uploads/ai-generated/{fileName}",
                FileSize = Encoding.UTF8.GetByteCount(content),
                MimeType = "text/plain",
                MatterId = matterId,
                Description = "AI tarafından oluşturuldu"
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("CREATE", "Document", document.Id, 
                details: "AI-generated document");

            return Json(new { success = true, documentId = document.Id });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // GET: AIDrafter/Templates
    public async Task<IActionResult> Templates()
    {
        var templates = await _context.DocumentTemplates
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Name)
            .ToListAsync();

        return View(templates);
    }

    // POST: AIDrafter/CreateTemplate
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTemplate(string name, string category, string content, string? description)
    {
        var template = new DocumentTemplate
        {
            Name = name,
            Category = category,
            Content = content,
            Description = description
        };

        _context.DocumentTemplates.Add(template);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("CREATE", "DocumentTemplate", template.Id);

        TempData["Success"] = "Şablon oluşturuldu.";
        return RedirectToAction(nameof(Templates));
    }
}
