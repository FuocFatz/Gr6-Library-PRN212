using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PRN212.LibraryManagement.Entities;

namespace PRN212.LibraryManagement.Data;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext()
    {
    }

    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<BorrowRecord> BorrowRecords => Set<BorrowRecord>();
    public DbSet<Payment> Payments => Set<Payment>();

    // Đọc chuỗi kết nối từ appsettings.json theo hướng dẫn của giảng viên.
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            string? connectionString = config["ConnectionStrings:DefaultConnection"];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Không tìm thấy ConnectionStrings:DefaultConnection trong appsettings.json.");
            }

            optionsBuilder.UseSqlServer(connectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<Book>(entity =>
        {
            entity.ToTable("Books");
            entity.HasKey(e => e.BookId);
            entity.Property(e => e.BookTitle).HasMaxLength(150).IsRequired();
            entity.Property(e => e.BookCategory).HasMaxLength(100).IsRequired();
            entity.Property(e => e.TotalQuantity).HasDefaultValue(0);
            entity.Property(e => e.AvailableQuantity).HasDefaultValue(0);
        });

        modelBuilder.Entity<BorrowRecord>(entity =>
        {
            entity.ToTable("BorrowRecords");
            entity.HasKey(e => e.BorrowId);
            entity.Property(e => e.BorrowDate)
                .HasColumnType("date")
                .HasDefaultValueSql("CONVERT(date, GETDATE())");
            entity.Property(e => e.DueDate).HasColumnType("date");
            entity.Property(e => e.ReturnDate).HasColumnType("date");
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Borrowed").IsRequired();
            entity.HasOne(e => e.User)
                .WithMany(e => e.BorrowRecords)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Book)
                .WithMany(e => e.BorrowRecords)
                .HasForeignKey(e => e.BookId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments");
            entity.HasKey(e => e.PaymentId);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PaymentWay).HasMaxLength(30).IsRequired();
            entity.Property(e => e.PaymentDate)
                .HasColumnType("datetime2(0)")
                .HasDefaultValueSql("SYSDATETIME()");
            entity.Property(e => e.PaymentStatus).HasMaxLength(20).HasDefaultValue("Completed").IsRequired();
            entity.HasOne(e => e.BorrowRecord)
                .WithOne(e => e.Payment)
                .HasForeignKey<Payment>(e => e.BorrowId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.BorrowId).IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }
}
