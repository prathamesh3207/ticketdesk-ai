using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TicketDesk.Api.Domain;

namespace TicketDesk.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<KbArticle> KbArticles => Set<KbArticle>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.FullName).HasMaxLength(120).IsRequired();
            e.Property(u => u.Email).HasMaxLength(200).IsRequired();
            e.Property(u => u.Role).HasMaxLength(20).IsRequired();
        });

        b.Entity<Ticket>(e =>
        {
            e.Property(t => t.Title).HasMaxLength(200).IsRequired();
            e.Property(t => t.Description).HasMaxLength(4000).IsRequired();
            e.Property(t => t.Status).HasMaxLength(20).IsRequired();
            e.Property(t => t.Priority).HasMaxLength(20).IsRequired();
            e.Property(t => t.Category).HasMaxLength(40).IsRequired();
            e.Property(t => t.Sentiment).HasMaxLength(20).IsRequired();
            e.Property(t => t.AiSummary).HasMaxLength(500);
            e.Property(t => t.AiSource).HasMaxLength(30);
            e.HasIndex(t => t.Status);
            e.HasIndex(t => t.Category);

            e.HasOne(t => t.CreatedBy).WithMany().HasForeignKey(t => t.CreatedById).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.AssignedTo).WithMany().HasForeignKey(t => t.AssignedToId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Comment>(e =>
        {
            e.Property(c => c.Body).HasMaxLength(4000).IsRequired();
            e.HasOne(c => c.Ticket).WithMany(t => t.Comments).HasForeignKey(c => c.TicketId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.Author).WithMany().HasForeignKey(c => c.AuthorId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<KbArticle>(e =>
        {
            e.Property(k => k.Title).HasMaxLength(200).IsRequired();
            e.Property(k => k.Tags).HasMaxLength(300);
        });

        // Make every DateTime come back flagged as UTC so the browser shows correct local time.
        var utc = new ValueConverter<DateTime, DateTime>(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        var utcNullable = new ValueConverter<DateTime?, DateTime?>(
            v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
        foreach (var entity in b.Model.GetEntityTypes())
        foreach (var prop in entity.GetProperties())
        {
            if (prop.ClrType == typeof(DateTime)) prop.SetValueConverter(utc);
            else if (prop.ClrType == typeof(DateTime?)) prop.SetValueConverter(utcNullable);
        }
    }
}
