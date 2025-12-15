using System.ComponentModel.DataAnnotations;

namespace JurisFlowASP.ViewModels;

// ===================== AUTH VIEW MODELS =====================
public class LoginViewModel
{
    [Required(ErrorMessage = "E-posta gerekli")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Şifre gerekli")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public bool RememberMe { get; set; } = false;
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Ad Soyad gerekli")]
    [StringLength(100)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "E-posta gerekli")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Şifre gerekli")]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor")]
    public string ConfirmPassword { get; set; } = "";

    public string Role { get; set; } = "Associate";
}

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "E-posta gerekli")]
    [EmailAddress]
    public string Email { get; set; } = "";
}

public class ResetPasswordViewModel
{
    [Required]
    public string Token { get; set; } = "";
    
    [Required]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Şifre gerekli")]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor")]
    public string ConfirmPassword { get; set; } = "";
}

// ===================== CLIENT VIEW MODELS =====================
public class ClientCreateViewModel
{
    [Required(ErrorMessage = "Müvekkil adı gerekli")]
    [StringLength(200)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "E-posta gerekli")]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Phone]
    public string? Phone { get; set; }

    public string? Mobile { get; set; }

    public string? Company { get; set; }

    [Required]
    public string Type { get; set; } = "Individual";

    public string Status { get; set; } = "Active";

    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
    public string? TaxId { get; set; }
    public string? Notes { get; set; }
    public bool PortalAccess { get; set; } = false;
    public string? PortalPassword { get; set; }
}

// ===================== MATTER VIEW MODELS =====================
public class MatterCreateViewModel
{
    [Required(ErrorMessage = "Dosya numarası gerekli")]
    public string CaseNumber { get; set; } = "";

    [Required(ErrorMessage = "Dava adı gerekli")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Müvekkil seçimi gerekli")]
    public string ClientId { get; set; } = "";

    [Required]
    public string PracticeArea { get; set; } = "";

    public string Status { get; set; } = "Open";

    public string FeeStructure { get; set; } = "Hourly";

    [Required(ErrorMessage = "Sorumlu avukat gerekli")]
    public string ResponsibleAttorney { get; set; } = "";

    [Range(0, 100000)]
    public decimal BillableRate { get; set; } = 0;

    [Range(0, 10000000)]
    public decimal TrustBalance { get; set; } = 0;
}

public class MatterListViewModel
{
    public string Id { get; set; } = "";
    public string CaseNumber { get; set; } = "";
    public string Name { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string PracticeArea { get; set; } = "";
    public string Status { get; set; } = "";
    public string ResponsibleAttorney { get; set; } = "";
    public DateTime OpenDate { get; set; }
    public decimal BillableRate { get; set; }
    public int TaskCount { get; set; }
    public int DocumentCount { get; set; }
}

// ===================== TASK VIEW MODELS =====================
public class TaskCreateViewModel
{
    [Required(ErrorMessage = "Görev başlığı gerekli")]
    public string Title { get; set; } = "";

    public string? Description { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DueDate { get; set; }

    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "To Do";
    public string? MatterId { get; set; }
    public string? AssignedToId { get; set; }
}

public class TaskBoardViewModel
{
    public List<TaskCardViewModel> ToDo { get; set; } = new();
    public List<TaskCardViewModel> InProgress { get; set; } = new();
    public List<TaskCardViewModel> Review { get; set; } = new();
    public List<TaskCardViewModel> Done { get; set; } = new();
}

public class TaskCardViewModel
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public string Priority { get; set; } = "";
    public string Status { get; set; } = "";
    public string? MatterName { get; set; }
    public string? AssignedTo { get; set; }
}

// ===================== INVOICE VIEW MODELS =====================
public class InvoiceCreateViewModel
{
    [Required]
    public string ClientId { get; set; } = "";

    [Required]
    public string Number { get; set; } = "";

    public string? MatterId { get; set; }

    [DataType(DataType.Date)]
    public DateTime DueDate { get; set; } = DateTime.Now.AddDays(30);

    public List<InvoiceLineItemViewModel> LineItems { get; set; } = new();

    public decimal TaxRate { get; set; } = 18;
    public decimal DiscountRate { get; set; } = 0;
    public string? Notes { get; set; }
}

public class InvoiceLineItemViewModel
{
    public string Description { get; set; } = "";
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; } = 0;
    public decimal Total => Quantity * UnitPrice;
}

// ===================== DOCUMENT VIEW MODELS =====================
public class DocumentUploadViewModel
{
    [Required(ErrorMessage = "Dosya gerekli")]
    public IFormFile? File { get; set; }

    public string? MatterId { get; set; }
    public string? Description { get; set; }
    public string? Tags { get; set; }
}

public class DocumentListViewModel
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string FileName { get; set; } = "";
    public int FileSize { get; set; }
    public string MimeType { get; set; } = "";
    public string? MatterName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string FileSizeFormatted => FormatFileSize(FileSize);

    private static string FormatFileSize(int bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1048576) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / 1048576.0:F1} MB";
    }
}

// ===================== TIME ENTRY VIEW MODELS =====================
public class TimeEntryCreateViewModel
{
    public string? MatterId { get; set; }

    [Required(ErrorMessage = "Açıklama gerekli")]
    public string Description { get; set; } = "";

    [Range(1, 1440)]
    public int Duration { get; set; } = 0; // Minutes

    [Range(0, 100000)]
    public decimal Rate { get; set; } = 0;

    [DataType(DataType.Date)]
    public DateTime Date { get; set; } = DateTime.Now;
}

// ===================== CALENDAR VIEW MODELS =====================
public class CalendarEventViewModel
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public DateTime Date { get; set; }
    public string Type { get; set; } = "";
    public string? MatterName { get; set; }
    public string Color => Type switch
    {
        "Court" => "#ef4444",
        "Meeting" => "#3b82f6",
        "Deadline" => "#f59e0b",
        _ => "#6b7280"
    };
}

// ===================== DASHBOARD VIEW MODELS =====================
public class DashboardViewModel
{
    public int TotalClients { get; set; }
    public int ActiveMatters { get; set; }
    public int PendingTasks { get; set; }
    public decimal TotalBilled { get; set; }
    public decimal TotalUnbilled { get; set; }
    public decimal OverdueInvoices { get; set; }
    
    public List<CalendarEventViewModel> UpcomingEvents { get; set; } = new();
    public List<TaskCardViewModel> RecentTasks { get; set; } = new();
    public List<MatterListViewModel> RecentMatters { get; set; } = new();
    
    // Charts data
    public Dictionary<string, int> MattersByStatus { get; set; } = new();
    public Dictionary<string, decimal> RevenueByMonth { get; set; } = new();
}

// ===================== LEAD VIEW MODELS =====================
public class LeadCreateViewModel
{
    [Required(ErrorMessage = "İsim gerekli")]
    public string Name { get; set; } = "";

    [EmailAddress]
    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Notes { get; set; }

    public string Source { get; set; } = "";
    public string Status { get; set; } = "New";

    [Range(0, 10000000)]
    public decimal EstimatedValue { get; set; } = 0;

    public string PracticeArea { get; set; } = "";
}

// ===================== SETTINGS VIEW MODELS =====================
public class UserProfileViewModel
{
    public string Id { get; set; } = "";

    [Required]
    public string Name { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? BarNumber { get; set; }
    public string? Bio { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Mevcut şifre gerekli")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "Yeni şifre gerekli")]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = "";

    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Şifreler eşleşmiyor")]
    public string ConfirmNewPassword { get; set; } = "";
}
