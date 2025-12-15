using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JurisFlowASP.Models;

// ===================== USER (Sistem Kullanıcısı) =====================
public class User
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = "";
    
    [Required, MaxLength(100)]
    public string Name { get; set; } = "";
    
    [Required, MaxLength(50)]
    public string Role { get; set; } = "Associate"; // Admin | Partner | Associate
    
    [Required]
    public string PasswordHash { get; set; } = "";
    
    [MaxLength(50)]
    public string? Phone { get; set; }
    
    [MaxLength(50)]
    public string? Mobile { get; set; }
    
    [MaxLength(255)]
    public string? Address { get; set; }
    
    [MaxLength(100)]
    public string? City { get; set; }
    
    [MaxLength(100)]
    public string? State { get; set; }
    
    [MaxLength(20)]
    public string? ZipCode { get; set; }
    
    [MaxLength(100)]
    public string? Country { get; set; }
    
    [MaxLength(50)]
    public string? BarNumber { get; set; }
    
    public string? Bio { get; set; }
    
    [MaxLength(500)]
    public string? Avatar { get; set; }
    
    public string? Preferences { get; set; } // JSON
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}

// ===================== CLIENT (Müvekkil) =====================
public class Client
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required, MaxLength(200)]
    public string Name { get; set; } = "";
    
    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = "";
    
    [MaxLength(50)]
    public string? Phone { get; set; }
    
    [MaxLength(50)]
    public string? Mobile { get; set; }
    
    [MaxLength(200)]
    public string? Company { get; set; }
    
    [Required, MaxLength(50)]
    public string Type { get; set; } = "Individual"; // Individual | Corporate
    
    [Required, MaxLength(50)]
    public string Status { get; set; } = "Active"; // Active | Inactive
    
    [MaxLength(500)]
    public string? Address { get; set; }
    
    [MaxLength(100)]
    public string? City { get; set; }
    
    [MaxLength(100)]
    public string? State { get; set; }
    
    [MaxLength(20)]
    public string? ZipCode { get; set; }
    
    [MaxLength(100)]
    public string? Country { get; set; }
    
    [MaxLength(50)]
    public string? TaxId { get; set; }
    
    public string? Notes { get; set; }
    
    // Client Portal
    public string? PortalPasswordHash { get; set; }
    public bool PortalAccess { get; set; } = false;
    public DateTime? LastLogin { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    public virtual ICollection<Matter> Matters { get; set; } = new List<Matter>();
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public virtual ICollection<ClientMessage> ClientMessages { get; set; } = new List<ClientMessage>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}

// ===================== MATTER (Dava Dosyası) =====================
public class Matter
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required, MaxLength(50)]
    public string CaseNumber { get; set; } = "";
    
    [Required, MaxLength(200)]
    public string Name { get; set; } = "";
    
    [Required, MaxLength(100)]
    public string PracticeArea { get; set; } = "";
    
    [Required, MaxLength(50)]
    public string Status { get; set; } = "Open"; // Open | Pending | Trial | Closed
    
    [Required, MaxLength(50)]
    public string FeeStructure { get; set; } = "Hourly"; // Hourly | Fixed | Contingency
    
    public DateTime OpenDate { get; set; } = DateTime.UtcNow;
    
    [Required, MaxLength(100)]
    public string ResponsibleAttorney { get; set; } = "";

    public string? Description { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal BillableRate { get; set; } = 0;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TrustBalance { get; set; } = 0;
    
    // Foreign Key
    [Required]
    public string ClientId { get; set; } = "";
    public virtual Client? Client { get; set; }
    
    // Navigation
    public virtual ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
    public virtual ICollection<CalendarEvent> Events { get; set; } = new List<CalendarEvent>();
    public virtual ICollection<ClientMessage> ClientMessages { get; set; } = new List<ClientMessage>();
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
}

// ===================== TASK (Görev) =====================
public class Task
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required, MaxLength(300)]
    public string Title { get; set; } = "";
    
    public string? Description { get; set; }
    
    public DateTime? DueDate { get; set; }
    
    public DateTime? ReminderAt { get; set; }
    
    [Required, MaxLength(20)]
    public string Priority { get; set; } = "Medium"; // High | Medium | Low
    
    [Required, MaxLength(50)]
    public string Status { get; set; } = "To Do"; // To Do | In Progress | Review | Done
    
    public string? MatterId { get; set; }
    public virtual Matter? Matter { get; set; }
    
    [MaxLength(100)]
    public string? AssignedToId { get; set; }
    public virtual User? AssignedToUser { get; set; }
    
    public string? TemplateId { get; set; }
    public virtual TaskTemplate? Template { get; set; }
    
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// ===================== TASK TEMPLATE (Görev Şablonu) =====================
public class TaskTemplate
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required, MaxLength(200)]
    public string Name { get; set; } = "";
    
    [MaxLength(100)]
    public string? Category { get; set; }
    
    public string? Description { get; set; }
    
    [Required]
    public string Definition { get; set; } = "[]"; // JSON
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
}

// ===================== TIME ENTRY (Zaman Kayıtları) =====================
public class TimeEntry
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string? MatterId { get; set; }
    public virtual Matter? Matter { get; set; }
    
    [Required]
    public string Description { get; set; } = "";
    
    public int Duration { get; set; } = 0; // Minutes
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Rate { get; set; } = 0;

    [NotMapped]
    public decimal Amount => (Duration / 60m) * Rate;
    
    public DateTime Date { get; set; } = DateTime.UtcNow;
    
    public bool IsBilled { get; set; } = false;
    
    [MaxLength(50)]
    public string Type { get; set; } = "time";
}

// ===================== EXPENSE (Gider Kaydı) =====================
public class Expense
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string? MatterId { get; set; }
    public virtual Matter? Matter { get; set; }
    
    [Required]
    public string Description { get; set; } = "";
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; } = 0;
    
    public DateTime Date { get; set; } = DateTime.UtcNow;
    
    [MaxLength(100)]
    public string Category { get; set; } = "Other";
    
    public bool IsBilled { get; set; } = false;
    
    [MaxLength(50)]
    public string Type { get; set; } = "expense";
}

// ===================== LEAD (CRM - Potansiyel Müşteri) =====================
public class Lead
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required, MaxLength(200)]
    public string Name { get; set; } = "";
    
    [MaxLength(100)]
    public string Source { get; set; } = "";
    
    [MaxLength(50)]
    public string Status { get; set; } = "New"; // New | Contacted | Converted | Lost
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal? EstimatedValue { get; set; } = 0;
    
    [MaxLength(100)]
    public string PracticeArea { get; set; } = "";

    [EmailAddress, MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }
}

// ===================== CALENDAR EVENT (Ajanda Etkinliği) =====================
public class CalendarEvent
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required, MaxLength(200)]
    public string Title { get; set; } = "";
    
    public DateTime Date { get; set; } = DateTime.UtcNow;
    
    [MaxLength(50)]
    public string Type { get; set; } = "Meeting"; // Meeting | Court | Deadline
    
    public string? MatterId { get; set; }
    public virtual Matter? Matter { get; set; }
}

// ===================== INVOICE (Fatura) =====================
public class Invoice
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required, MaxLength(50)]
    public string Number { get; set; } = "";
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; } = 0;
    
    public DateTime DueDate { get; set; } = DateTime.UtcNow;
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    
    [MaxLength(50)]
    public string Status { get; set; } = "Draft"; // Paid | Overdue | Draft | Sent
    
    [Required]
    public string ClientId { get; set; } = "";
    public virtual Client? Client { get; set; }

    public string? Notes { get; set; }
    public virtual ICollection<TimeEntry> Items { get; set; } = new List<TimeEntry>();
}

// ===================== NOTIFICATION (Bildirim) =====================
public class Notification
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string? UserId { get; set; }
    public virtual User? User { get; set; }
    
    public string? ClientId { get; set; }
    public virtual Client? Client { get; set; }
    
    [Required, MaxLength(200)]
    public string Title { get; set; } = "";
    
    [Required]
    public string Message { get; set; } = "";
    
    [MaxLength(50)]
    public string Type { get; set; } = "info"; // info | warning | error | success
    
    public bool Read { get; set; } = false;
    
    [MaxLength(500)]
    public string? Link { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ===================== CLIENT MESSAGE (Müvekkil Mesajı) =====================
public class ClientMessage
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string ClientId { get; set; } = "";
    public virtual Client? Client { get; set; }
    
    public string? MatterId { get; set; }
    public virtual Matter? Matter { get; set; }
    
    [Required, MaxLength(300)]
    public string Subject { get; set; } = "";
    
    [Required]
    public string Message { get; set; } = "";
    
    public bool Read { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ===================== DOCUMENT (Doküman) =====================
public class Document
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required, MaxLength(300)]
    public string Name { get; set; } = "";
    
    [Required, MaxLength(300)]
    public string FileName { get; set; } = "";
    
    [Required, MaxLength(500)]
    public string FilePath { get; set; } = "";
    
    public int FileSize { get; set; } = 0;
    
    [MaxLength(100)]
    public string MimeType { get; set; } = "";
    
    public string? MatterId { get; set; }
    public virtual Matter? Matter { get; set; }
    
    public string? UploadedBy { get; set; }
    
    public int Version { get; set; } = 1;
    
    [MaxLength(200)]
    public string? GroupKey { get; set; }
    
    public string? Description { get; set; }
    
    public string? Tags { get; set; } // JSON
    
    public string? TextContent { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// ===================== AUDIT LOG (İşlem Kayıtları) =====================
public class AuditLog
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string? UserId { get; set; }
    
    [MaxLength(255)]
    public string? UserEmail { get; set; }
    
    public string? ClientId { get; set; }
    
    [MaxLength(255)]
    public string? ClientEmail { get; set; }
    
    [Required, MaxLength(50)]
    public string Action { get; set; } = "";
    
    [Required, MaxLength(100)]
    public string EntityType { get; set; } = "";
    
    public string? EntityId { get; set; }
    
    public string? OldValues { get; set; } // JSON
    
    public string? NewValues { get; set; } // JSON
    
    public string? Details { get; set; }
    
    [MaxLength(50)]
    public string? IpAddress { get; set; }
    
    public string? UserAgent { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ===================== PASSWORD RESET TOKEN =====================
public class PasswordResetToken
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = "";
    
    [Required, MaxLength(500)]
    public string Token { get; set; } = "";
    
    public DateTime ExpiresAt { get; set; }
    
    public bool Used { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ===================== DOCUMENT TEMPLATE (Doküman Şablonu) =====================
public class DocumentTemplate
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required, MaxLength(200)]
    public string Name { get; set; } = "";
    
    [Required, MaxLength(100)]
    public string Category { get; set; } = "";
    
    public string? Description { get; set; }
    
    [Required]
    public string Content { get; set; } = "";
    
    public string? Variables { get; set; } // JSON
    
    public bool IsActive { get; set; } = true;
    
    public string? CreatedBy { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// ===================== REMINDER (Hatırlatıcı) =====================
public class Reminder
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [MaxLength(50)]
    public string Type { get; set; } = "notification"; // email, sms, notification
    
    public DateTime TriggerAt { get; set; }
    
    public bool Sent { get; set; } = false;
    
    public DateTime? SentAt { get; set; }
    
    [MaxLength(100)]
    public string EntityType { get; set; } = "";
    
    public string EntityId { get; set; } = "";
    
    public string? UserId { get; set; }
    
    public string? Message { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
