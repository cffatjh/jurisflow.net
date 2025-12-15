using System.Net;
using System.Net.Mail;

namespace JurisFlowASP.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task SendPasswordResetEmailAsync(string to, string resetLink);
    Task SendInvoiceEmailAsync(string to, string invoiceNumber, decimal amount);
    Task SendTaskReminderAsync(string to, string taskTitle, DateTime dueDate);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        try
        {
            var host = _configuration["SmtpSettings:Host"];
            var port = int.Parse(_configuration["SmtpSettings:Port"] ?? "587");
            var username = _configuration["SmtpSettings:Username"];
            var password = _configuration["SmtpSettings:Password"];
            var fromEmail = _configuration["SmtpSettings:FromEmail"];
            var fromName = _configuration["SmtpSettings:FromName"];

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("SMTP not configured. Email not sent to {Email}", to);
                return;
            }

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail ?? username, fromName ?? "JurisFlow"),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };
            message.To.Add(to);

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent to {Email}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", to);
        }
    }

    public async Task SendPasswordResetEmailAsync(string to, string resetLink)
    {
        var subject = "JurisFlow - Şifre Sıfırlama";
        var body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background: linear-gradient(135deg, #1e293b 0%, #334155 100%); padding: 30px; text-align: center;'>
                    <h1 style='color: #fff; margin: 0;'>JurisFlow</h1>
                </div>
                <div style='padding: 30px; background: #f8fafc;'>
                    <h2 style='color: #1e293b;'>Şifre Sıfırlama Talebi</h2>
                    <p>Şifrenizi sıfırlamak için aşağıdaki bağlantıya tıklayın:</p>
                    <a href='{resetLink}' style='display: inline-block; background: #3b82f6; color: #fff; padding: 12px 24px; text-decoration: none; border-radius: 6px; margin: 20px 0;'>Şifremi Sıfırla</a>
                    <p style='color: #64748b; font-size: 14px;'>Bu bağlantı 24 saat geçerlidir.</p>
                    <p style='color: #64748b; font-size: 14px;'>Bu talebi siz yapmadıysanız, bu e-postayı görmezden gelebilirsiniz.</p>
                </div>
            </div>";

        await SendEmailAsync(to, subject, body);
    }

    public async Task SendInvoiceEmailAsync(string to, string invoiceNumber, decimal amount)
    {
        var subject = $"JurisFlow - Fatura #{invoiceNumber}";
        var body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background: linear-gradient(135deg, #1e293b 0%, #334155 100%); padding: 30px; text-align: center;'>
                    <h1 style='color: #fff; margin: 0;'>JurisFlow</h1>
                </div>
                <div style='padding: 30px; background: #f8fafc;'>
                    <h2 style='color: #1e293b;'>Fatura Bildirimi</h2>
                    <p>Sayın Müvekkilimiz,</p>
                    <p><strong>Fatura No:</strong> {invoiceNumber}</p>
                    <p><strong>Tutar:</strong> ₺{amount:N2}</p>
                    <p>Faturanızı görüntülemek için JurisFlow hesabınıza giriş yapabilirsiniz.</p>
                </div>
            </div>";

        await SendEmailAsync(to, subject, body);
    }

    public async Task SendTaskReminderAsync(string to, string taskTitle, DateTime dueDate)
    {
        var subject = "JurisFlow - Görev Hatırlatması";
        var body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background: linear-gradient(135deg, #1e293b 0%, #334155 100%); padding: 30px; text-align: center;'>
                    <h1 style='color: #fff; margin: 0;'>JurisFlow</h1>
                </div>
                <div style='padding: 30px; background: #f8fafc;'>
                    <h2 style='color: #1e293b;'>Görev Hatırlatması</h2>
                    <p><strong>Görev:</strong> {taskTitle}</p>
                    <p><strong>Bitiş Tarihi:</strong> {dueDate:dd.MM.yyyy}</p>
                    <p>Görevinizi tamamlamayı unutmayın!</p>
                </div>
            </div>";

        await SendEmailAsync(to, subject, body);
    }
}
