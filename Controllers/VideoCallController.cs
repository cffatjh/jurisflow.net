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
public class VideoCallController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IAuditService _auditService;
    private readonly IHttpClientFactory _httpClientFactory;

    public VideoCallController(
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

    // GET: VideoCall
    public async Task<IActionResult> Index()
    {
        var clients = await _context.Clients.OrderBy(c => c.Name).ToListAsync();
        var matters = await _context.Matters.Include(m => m.Client).OrderByDescending(m => m.OpenDate).ToListAsync();

        ViewBag.Clients = clients;
        ViewBag.Matters = matters;

        // Check if integrations are configured
        ViewBag.ZoomConfigured = !string.IsNullOrEmpty(_configuration["Zoom:ClientId"]);
        ViewBag.GoogleConfigured = !string.IsNullOrEmpty(_configuration["Google:ClientId"]);

        return View();
    }

    // POST: VideoCall/CreateZoomMeeting
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateZoomMeeting(string topic, DateTime startTime, int duration, string? matterId, string? clientEmail)
    {
        try
        {
            var accessToken = _configuration["Zoom:AccessToken"];
            if (string.IsNullOrEmpty(accessToken))
            {
                return Json(new { success = false, error = "Zoom access token yapılandırılmamış. appsettings.json dosyasına Zoom bilgilerini ekleyin." });
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            var requestBody = new
            {
                topic = topic,
                type = 2, // Scheduled meeting
                start_time = startTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                duration = duration,
                timezone = "Europe/Istanbul",
                settings = new
                {
                    host_video = true,
                    participant_video = true,
                    join_before_host = true,
                    waiting_room = false
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.zoom.us/v2/users/me/meetings", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, error = $"Zoom API hatası: {response.StatusCode}" });
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseContent);

            var meetingUrl = doc.RootElement.GetProperty("join_url").GetString();
            var meetingId = doc.RootElement.GetProperty("id").GetInt64();

            await _auditService.LogAsync("CREATE_ZOOM_MEETING", "VideoCall", meetingId.ToString(),
                details: $"Topic: {topic}, Duration: {duration}min");

            return Json(new
            {
                success = true,
                meetingUrl = meetingUrl,
                meetingId = meetingId,
                provider = "zoom"
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // POST: VideoCall/CreateGoogleMeet
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGoogleMeet(string summary, DateTime startTime, int duration, string? matterId, string? attendeeEmail)
    {
        try
        {
            var accessToken = _configuration["Google:AccessToken"];
            if (string.IsNullOrEmpty(accessToken))
            {
                return Json(new { success = false, error = "Google access token yapılandırılmamış. OAuth akışını tamamlayın." });
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            var endTime = startTime.AddMinutes(duration);

            var requestBody = new
            {
                summary = summary,
                start = new { dateTime = startTime.ToString("yyyy-MM-ddTHH:mm:ss"), timeZone = "Europe/Istanbul" },
                end = new { dateTime = endTime.ToString("yyyy-MM-ddTHH:mm:ss"), timeZone = "Europe/Istanbul" },
                conferenceData = new
                {
                    createRequest = new
                    {
                        requestId = Guid.NewGuid().ToString(),
                        conferenceSolutionKey = new { type = "hangoutsMeet" }
                    }
                },
                attendees = !string.IsNullOrEmpty(attendeeEmail) 
                    ? new[] { new { email = attendeeEmail } } 
                    : Array.Empty<object>()
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(
                "https://www.googleapis.com/calendar/v3/calendars/primary/events?conferenceDataVersion=1",
                content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, error = $"Google API hatası: {response.StatusCode}" });
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseContent);

            var meetingUrl = doc.RootElement
                .GetProperty("conferenceData")
                .GetProperty("entryPoints")[0]
                .GetProperty("uri")
                .GetString();

            var eventId = doc.RootElement.GetProperty("id").GetString();

            await _auditService.LogAsync("CREATE_GOOGLE_MEET", "VideoCall", eventId,
                details: $"Summary: {summary}, Duration: {duration}min");

            return Json(new
            {
                success = true,
                meetingUrl = meetingUrl,
                eventId = eventId,
                provider = "google"
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    // GET: VideoCall/ZoomAuth
    public IActionResult ZoomAuth()
    {
        var clientId = _configuration["Zoom:ClientId"];
        var redirectUri = $"{Request.Scheme}://{Request.Host}/VideoCall/ZoomCallback";

        var authUrl = $"https://zoom.us/oauth/authorize?response_type=code&client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}";

        return Redirect(authUrl);
    }

    // GET: VideoCall/ZoomCallback
    public async Task<IActionResult> ZoomCallback(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            TempData["Error"] = "Zoom yetkilendirme başarısız.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var clientId = _configuration["Zoom:ClientId"];
            var clientSecret = _configuration["Zoom:ClientSecret"];
            var redirectUri = $"{Request.Scheme}://{Request.Host}/VideoCall/ZoomCallback";

            var client = _httpClientFactory.CreateClient();
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");

            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", redirectUri)
            });

            var response = await client.PostAsync("https://zoom.us/oauth/token", tokenRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseContent);
                var accessToken = doc.RootElement.GetProperty("access_token").GetString();

                // In production, store this securely (encrypted in DB or secure storage)
                TempData["Success"] = "Zoom bağlantısı başarılı! Access token alındı.";
                TempData["ZoomToken"] = accessToken; // For demo - store securely in production
            }
            else
            {
                TempData["Error"] = "Zoom token alınamadı.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Hata: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: VideoCall/GoogleAuth
    public IActionResult GoogleAuth()
    {
        var clientId = _configuration["Google:ClientId"];
        var redirectUri = $"{Request.Scheme}://{Request.Host}/VideoCall/GoogleCallback";
        var scope = "https://www.googleapis.com/auth/calendar.events";

        var authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?response_type=code&client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope={Uri.EscapeDataString(scope)}&access_type=offline";

        return Redirect(authUrl);
    }

    // GET: VideoCall/GoogleCallback
    public async Task<IActionResult> GoogleCallback(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            TempData["Error"] = "Google yetkilendirme başarısız.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var clientId = _configuration["Google:ClientId"];
            var clientSecret = _configuration["Google:ClientSecret"];
            var redirectUri = $"{Request.Scheme}://{Request.Host}/VideoCall/GoogleCallback";

            var client = _httpClientFactory.CreateClient();

            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", redirectUri),
                new KeyValuePair<string, string>("client_id", clientId!),
                new KeyValuePair<string, string>("client_secret", clientSecret!)
            });

            var response = await client.PostAsync("https://oauth2.googleapis.com/token", tokenRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseContent);
                var accessToken = doc.RootElement.GetProperty("access_token").GetString();

                TempData["Success"] = "Google bağlantısı başarılı!";
                TempData["GoogleToken"] = accessToken;
            }
            else
            {
                TempData["Error"] = "Google token alınamadı.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Hata: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }
}
