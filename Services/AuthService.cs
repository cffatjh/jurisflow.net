using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using JurisFlowASP.Data;
using JurisFlowASP.Models;
using Task = System.Threading.Tasks.Task;

namespace JurisFlowASP.Services;

public interface IAuthService
{
    Task<User?> ValidateUserAsync(string email, string password);
    Task<User?> GetUserByIdAsync(string userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<bool> CreateUserAsync(User user, string password);
    Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<string> GeneratePasswordResetTokenAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
    Task EnsureAdminAsync();
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<User?> ValidateUserAsync(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return null;
        
        if (!VerifyPassword(password, user.PasswordHash))
            return null;
            
        return user;
    }

    public async Task<User?> GetUserByIdAsync(string userId)
    {
        return await _context.Users.FindAsync(userId);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> CreateUserAsync(User user, string password)
    {
        if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            return false;

        user.PasswordHash = HashPassword(password);
        user.CreatedAt = DateTime.UtcNow;
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        if (!VerifyPassword(currentPassword, user.PasswordHash))
            return false;

        user.PasswordHash = HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<string> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return "";

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        
        var resetToken = new PasswordResetToken
        {
            Email = email,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        _context.PasswordResetTokens.Add(resetToken);
        await _context.SaveChangesAsync();

        return token;
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var resetToken = await _context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.Token == token && !t.Used && t.ExpiresAt > DateTime.UtcNow);

        if (resetToken == null) return false;

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == resetToken.Email);
        if (user == null) return false;

        user.PasswordHash = HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        
        resetToken.Used = true;
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async System.Threading.Tasks.Task EnsureAdminAsync()
    {
        var adminEmail = _configuration["AdminSettings:Email"] ?? "admin@jurisflow.com";
        var adminPassword = _configuration["AdminSettings:Password"] ?? "Admin123!";

        if (!await _context.Users.AnyAsync(u => u.Email == adminEmail))
        {
            var admin = new User
            {
                Email = adminEmail,
                Name = "Admin",
                Role = "Admin",
                PasswordHash = HashPassword(adminPassword)
            };
            _context.Users.Add(admin);
            await _context.SaveChangesAsync();
        }
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, 12);
    }

    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
}
