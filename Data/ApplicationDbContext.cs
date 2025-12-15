using Microsoft.EntityFrameworkCore;
using JurisFlowASP.Models;

namespace JurisFlowASP.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets - All 17 tables
    public DbSet<User> Users { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Matter> Matters { get; set; }
    public DbSet<Models.Task> Tasks { get; set; }
    public DbSet<TaskTemplate> TaskTemplates { get; set; }
    public DbSet<TimeEntry> TimeEntries { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<Lead> Leads { get; set; }
    public DbSet<CalendarEvent> CalendarEvents { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<ClientMessage> ClientMessages { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<DocumentTemplate> DocumentTemplates { get; set; }
    public DbSet<Reminder> Reminders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Client -> Matter (One to Many)
        modelBuilder.Entity<Matter>()
            .HasOne(m => m.Client)
            .WithMany(c => c.Matters)
            .HasForeignKey(m => m.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        // Matter -> TimeEntry
        modelBuilder.Entity<TimeEntry>()
            .HasOne(t => t.Matter)
            .WithMany(m => m.TimeEntries)
            .HasForeignKey(t => t.MatterId)
            .OnDelete(DeleteBehavior.SetNull);

        // Matter -> Expense
        modelBuilder.Entity<Expense>()
            .HasOne(e => e.Matter)
            .WithMany(m => m.Expenses)
            .HasForeignKey(e => e.MatterId)
            .OnDelete(DeleteBehavior.SetNull);

        // Matter -> Task
        modelBuilder.Entity<Models.Task>()
            .HasOne(t => t.Matter)
            .WithMany(m => m.Tasks)
            .HasForeignKey(t => t.MatterId)
            .OnDelete(DeleteBehavior.SetNull);

        // TaskTemplate -> Task
        modelBuilder.Entity<Models.Task>()
            .HasOne(t => t.Template)
            .WithMany(tt => tt.Tasks)
            .HasForeignKey(t => t.TemplateId)
            .OnDelete(DeleteBehavior.SetNull);

        // Matter -> CalendarEvent
        modelBuilder.Entity<CalendarEvent>()
            .HasOne(e => e.Matter)
            .WithMany(m => m.Events)
            .HasForeignKey(e => e.MatterId)
            .OnDelete(DeleteBehavior.SetNull);

        // Client -> Invoice
        modelBuilder.Entity<Invoice>()
            .HasOne(i => i.Client)
            .WithMany(c => c.Invoices)
            .HasForeignKey(i => i.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        // User -> Notification
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Client -> Notification
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Client)
            .WithMany(c => c.Notifications)
            .HasForeignKey(n => n.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        // ClientMessage relationships
        modelBuilder.Entity<ClientMessage>()
            .HasOne(m => m.Client)
            .WithMany(c => c.ClientMessages)
            .HasForeignKey(m => m.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ClientMessage>()
            .HasOne(m => m.Matter)
            .WithMany(mt => mt.ClientMessages)
            .HasForeignKey(m => m.MatterId)
            .OnDelete(DeleteBehavior.SetNull);

        // Document -> Matter
        modelBuilder.Entity<Document>()
            .HasOne(d => d.Matter)
            .WithMany(m => m.Documents)
            .HasForeignKey(d => d.MatterId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique indexes
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Client>()
            .HasIndex(c => c.Email)
            .IsUnique();

        modelBuilder.Entity<PasswordResetToken>()
            .HasIndex(p => p.Token)
            .IsUnique();
    }
}
