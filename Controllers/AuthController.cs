using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JurisFlowASP.Services;
using JurisFlowASP.ViewModels;

namespace JurisFlowASP.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly IAuditService _auditService;
    private readonly IEmailService _emailService;

    public AuthController(IAuthService authService, IAuditService auditService, IEmailService emailService)
    {
        _authService = authService;
        _auditService = auditService;
        _emailService = emailService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _authService.ValidateUserAsync(model.Email, model.Password);
        if (user == null)
        {
            ModelState.AddModelError("", "Geçersiz e-posta veya şifre");
            return View(model);
        }

        // Create claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("Initials", GetInitials(user.Name))
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(model.RememberMe ? 30 : 1)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        await _auditService.LogAsync("LOGIN", "User", user.Id, details: "User logged in");

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await _auditService.LogAsync("LOGOUT", "User", userId, details: "User logged out");
        
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var token = await _authService.GeneratePasswordResetTokenAsync(model.Email);
        if (!string.IsNullOrEmpty(token))
        {
            var resetLink = Url.Action("ResetPassword", "Auth", new { token }, Request.Scheme);
            await _emailService.SendPasswordResetEmailAsync(model.Email, resetLink ?? "");
        }

        // Always show success to prevent email enumeration
        TempData["Message"] = "Şifre sıfırlama bağlantısı e-posta adresinize gönderildi.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult ResetPassword(string token)
    {
        if (string.IsNullOrEmpty(token))
            return RedirectToAction("Login");
            
        return View(new ResetPasswordViewModel { Token = token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _authService.ResetPasswordAsync(model.Token, model.Password);
        if (!result)
        {
            ModelState.AddModelError("", "Geçersiz veya süresi dolmuş token");
            return View(model);
        }

        TempData["Message"] = "Şifreniz başarıyla sıfırlandı. Giriş yapabilirsiniz.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private static string GetInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
            return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
        return name.Length >= 2 ? name[..2].ToUpper() : name.ToUpper();
    }
}
