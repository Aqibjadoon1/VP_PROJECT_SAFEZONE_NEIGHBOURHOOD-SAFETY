using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SafeZone.Server.Models;

namespace SafeZone.Server.Data;

public class SafeZoneDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public SafeZoneDbContext(DbContextOptions<SafeZoneDbContext> options) : base(options) { }

    public DbSet<Incident> Incidents { get; set; }
    public DbSet<Authority> Authorities { get; set; }
    public DbSet<IncidentCategory> IncidentCategories { get; set; }
    public DbSet<FIRReport> FIRReports { get; set; }
    public DbSet<AICallLog> AICallLogs { get; set; }
    public DbSet<Alert> Alerts { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Response> Responses { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ========== ENUM MAPPINGS ==========
        builder.Entity<User>()
            .Property(u => u.Role)
            .HasConversion<string>();

        builder.Entity<Incident>()
            .Property(i => i.Status)
            .HasConversion<string>();

        builder.Entity<Incident>()
            .Property(i => i.Severity)
            .HasConversion<string>();

        builder.Entity<FIRReport>()
            .Property(f => f.Status)
            .HasConversion<string>();

        builder.Entity<Authority>()
            .Property(a => a.Type)
            .HasConversion<string>();

        builder.Entity<AICallLog>()
            .Property(c => c.Status)
            .HasConversion<string>();

        builder.Entity<Alert>()
            .Property(a => a.Type)
            .HasConversion<string>();

        builder.Entity<Alert>()
            .Property(a => a.Scope)
            .HasConversion<string>();

        // ========== RENAME IDENTITY TABLES (for clarity) ==========
        builder.Entity<User>(b =>
        {
            b.ToTable("Users");
        });

        builder.Entity<IdentityRole<Guid>>(b =>
        {
            b.ToTable("Roles");
        });

        builder.Entity<IdentityUserRole<Guid>>(b =>
        {
            b.ToTable("UserRoles");
            b.HasKey(ur => new { ur.UserId, ur.RoleId });
        });

        builder.Entity<IdentityUserClaim<Guid>>(b =>
        {
            b.ToTable("UserClaims");
        });

        builder.Entity<IdentityUserLogin<Guid>>(b =>
        {
            b.ToTable("UserLogins");
            b.HasKey(l => new { l.LoginProvider, l.ProviderKey });
        });

        builder.Entity<IdentityRoleClaim<Guid>>(b =>
        {
            b.ToTable("RoleClaims");
        });

        builder.Entity<IdentityUserToken<Guid>>(b =>
        {
            b.ToTable("UserTokens");
            b.HasKey(t => new { t.UserId, t.LoginProvider, t.Name });
        });

        // ========== ENTITY CONFIGURATION + INDEXES ==========

        // --- IncidentCategory (lookup, seeded) ---
        builder.Entity<IncidentCategory>(b =>
        {
            b.HasKey(c => c.CategoryId);
            b.HasIndex(c => c.Name).IsUnique();
        });

        // --- Authority (police/rescue worker profile) ---
        builder.Entity<Authority>(b =>
        {
            b.HasKey(a => a.AuthId);
            b.HasOne(a => a.User)
                .WithOne(u => u.AuthorityProfile)
                .HasForeignKey<Authority>(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(a => a.UserId).IsUnique();
            b.HasIndex(a => a.Type);
            b.HasIndex(a => a.UnitName);
            b.HasIndex(a => new { a.JurisdictionCenterLat, a.JurisdictionCenterLng });
        });

        // --- Incident (core entity) ---
        builder.Entity<Incident>(b =>
        {
            b.HasKey(i => i.IncidentId);

            b.HasOne(i => i.Category)
                .WithMany(c => c.Incidents)
                .HasForeignKey(i => i.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(i => i.Reporter)
                .WithMany(u => u.ReportedIncidents)
                .HasForeignKey(i => i.ReporterId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(i => i.FIR)
                .WithOne(f => f.Incident)
                .HasForeignKey<FIRReport>(f => f.IncidentId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(i => i.IncidentNumber).IsUnique();
            b.HasIndex(i => i.Status);
            b.HasIndex(i => i.Severity);
            b.HasIndex(i => i.ReportedAt);
            b.HasIndex(i => new { i.Latitude, i.Longitude });
            b.HasIndex(i => i.CategoryId);
            b.HasIndex(i => i.ReporterId);
            b.HasIndex(i => i.AssignedAuthorityId);
        });

        // --- FIRReport ---
        builder.Entity<FIRReport>(b =>
        {
            b.HasKey(f => f.FIRId);

            b.HasOne(f => f.Reporter)
                .WithMany(u => u.FIRReports)
                .HasForeignKey(f => f.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(f => f.FIRNumber).IsUnique();
            b.HasIndex(f => f.Status);
            b.HasIndex(f => f.SubmittedAt);
            b.HasIndex(f => f.IncidentId);
            b.HasIndex(f => f.ComplainantCNIC);
        });

        // --- AICallLog ---
        builder.Entity<AICallLog>(b =>
        {
            b.HasKey(c => c.LogId);
            b.HasIndex(c => c.TwilioCallSid).IsUnique().HasFilter("[TwilioCallSid] IS NOT NULL");
            b.HasIndex(c => c.Status);
            b.HasIndex(c => c.InitiatedAt);
            b.HasIndex(c => c.IncidentId);
        });

        // --- Alert ---
        builder.Entity<Alert>(b =>
        {
            b.HasKey(a => a.AlertId);
            b.HasIndex(a => a.Type);
            b.HasIndex(a => a.Scope);
            b.HasIndex(a => a.IssuedAt);
            b.HasIndex(a => a.IsActive);
            b.HasIndex(a => new { a.CenterLat, a.CenterLng });
        });

        // --- Notification ---
        builder.Entity<Notification>(b =>
        {
            b.HasKey(n => n.NotificationId);

            b.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(n => n.UserId);
            b.HasIndex(n => n.IsRead);
            b.HasIndex(n => n.CreatedAt);
        });

        // --- Comment ---
        builder.Entity<Comment>(b =>
        {
            b.HasKey(c => c.CommentId);

            b.HasOne(c => c.Incident)
                .WithMany(i => i.Comments)
                .HasForeignKey(c => c.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(c => c.IncidentId);
            b.HasIndex(c => c.CreatedAt);
        });

        // --- Response ---
        builder.Entity<Response>(b =>
        {
            b.HasKey(r => r.ResponseId);

            b.HasOne(r => r.Incident)
                .WithMany(i => i.Responses)
                .HasForeignKey(r => r.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(r => r.Authority)
                .WithMany(a => a.Responses)
                .HasForeignKey(r => r.AuthorityId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(r => r.IncidentId);
            b.HasIndex(r => r.AuthorityId);
            b.HasIndex(r => r.RespondedAt);
        });
    }
}
