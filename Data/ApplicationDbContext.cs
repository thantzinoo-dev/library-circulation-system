using Microsoft.EntityFrameworkCore;
using School_Library_Management.Models;

namespace School_Library_Management.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<BorrowRecord> BorrowRecords => Set<BorrowRecord>();
    public DbSet<LibrarySettings> LibrarySettings => Set<LibrarySettings>();
    public DbSet<AdminProfile> AdminProfiles => Set<AdminProfile>();
    public DbSet<AdminAccount> AdminAccounts => Set<AdminAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasIndex(b => b.ISBN)
                  .IsUnique()
                  .HasFilter("[ISBN] IS NOT NULL");

            entity.Property(b => b.TotalCopies).HasDefaultValue(1);
            entity.Property(b => b.AvailableCopies).HasDefaultValue(1);
            entity.Property(b => b.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(b => b.CoverImagePath).HasMaxLength(300);

            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_Books_PublishedYear",
                    "[PublishedYear] BETWEEN 1450 AND 2100");
                table.HasCheckConstraint(
                    "CK_Books_CopyCounts",
                    "[TotalCopies] > 0 AND [AvailableCopies] >= 0 AND [AvailableCopies] <= [TotalCopies]");
            });
        });

        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasIndex(m => m.StudentId).IsUnique();

            entity.Property(m => m.MembershipType)
                  .HasMaxLength(20)
                  .HasDefaultValue(MemberType.Student);
            entity.Property(m => m.IsActive).HasDefaultValue(true);
            entity.Property(m => m.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<BorrowRecord>(entity =>
        {
            entity.Property(r => r.Status)
                  .HasDefaultValue(BorrowStatus.Borrowed);

            entity.Property(r => r.FineAmount)
                  .HasPrecision(18, 2)
                  .HasDefaultValue(0m);

            entity.Property(r => r.Quantity)
                  .HasDefaultValue(1);

            entity.Property(r => r.Notes)
                  .HasMaxLength(500);

            entity.Property(r => r.ReturnCondition)
                  .HasMaxLength(50);

            entity.Property(r => r.BorrowDate)
                  .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasOne(r => r.Book)
                  .WithMany(b => b.BorrowRecords)
                  .HasForeignKey(r => r.BookId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Member)
                  .WithMany(m => m.BorrowRecords)
                  .HasForeignKey(r => r.MemberId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_BorrowRecords_Status",
                    $"[Status] IN (N'{BorrowStatus.Borrowed}', N'{BorrowStatus.Returned}', N'{BorrowStatus.Overdue}')");
                table.HasCheckConstraint(
                    "CK_BorrowRecords_FineAmount",
                    "[FineAmount] >= 0");
                table.HasCheckConstraint(
                    "CK_BorrowRecords_Quantity",
                    "[Quantity] > 0");
                table.HasCheckConstraint(
                    "CK_BorrowRecords_Dates",
                    "[DueDate] >= [BorrowDate] AND ([ReturnDate] IS NULL OR [ReturnDate] >= [BorrowDate])");
                table.HasCheckConstraint(
                    "CK_BorrowRecords_ReturnStatus",
                    $"([Status] = N'{BorrowStatus.Returned}' AND [ReturnDate] IS NOT NULL) OR " +
                    $"([Status] <> N'{BorrowStatus.Returned}' AND [ReturnDate] IS NULL)");
            });
        });

        modelBuilder.Entity<LibrarySettings>(entity =>
        {
            entity.Property(setting => setting.DefaultReportPeriod)
                  .HasMaxLength(20)
                  .HasDefaultValue(ReportPeriod.ThisMonth);
            entity.Property(setting => setting.Theme)
                  .HasMaxLength(20)
                  .HasDefaultValue(ThemePreference.System);
            entity.Property(setting => setting.ShowPdfExport).HasDefaultValue(true);
            entity.Property(setting => setting.ShowExcelExport).HasDefaultValue(true);
            entity.Property(setting => setting.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_LibrarySettings_ReportPeriod",
                    $"[DefaultReportPeriod] IN (N'{ReportPeriod.ThisMonth}', N'{ReportPeriod.LastMonth}', N'{ReportPeriod.ThisYear}', N'{ReportPeriod.All}')");
                table.HasCheckConstraint(
                    "CK_LibrarySettings_Theme",
                    $"[Theme] IN (N'{ThemePreference.System}', N'{ThemePreference.Light}', N'{ThemePreference.Dark}')");
            });
        });

        modelBuilder.Entity<AdminProfile>(entity =>
        {
            entity.Property(profile => profile.FullName).HasMaxLength(100);
            entity.Property(profile => profile.Role).HasMaxLength(60);
            entity.Property(profile => profile.Email).HasMaxLength(256);
            entity.Property(profile => profile.Phone).HasMaxLength(30);
            entity.Property(profile => profile.Department).HasMaxLength(100);
            entity.Property(profile => profile.Bio).HasMaxLength(500);
            entity.Property(profile => profile.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<AdminAccount>(entity =>
        {
            entity.HasIndex(account => account.NormalizedUsername).IsUnique();
            entity.HasIndex(account => account.NormalizedEmail).IsUnique();
            entity.Property(account => account.Username).HasMaxLength(50);
            entity.Property(account => account.NormalizedUsername).HasMaxLength(50);
            entity.Property(account => account.Email).HasMaxLength(256);
            entity.Property(account => account.NormalizedEmail).HasMaxLength(256);
            entity.Property(account => account.PasswordHash).HasMaxLength(500);
            entity.Property(account => account.IsActive).HasDefaultValue(true);
            entity.Property(account => account.MustChangePassword).HasDefaultValue(true);
            entity.Property(account => account.FailedLoginAttempts).HasDefaultValue(0);
            entity.Property(account => account.CreatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(account => account.UpdatedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            entity.ToTable(table => table.HasCheckConstraint(
                "CK_AdminAccounts_FailedLoginAttempts",
                "[FailedLoginAttempts] >= 0"));
        });
    }
}
