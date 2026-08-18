using Microsoft.EntityFrameworkCore;
using Ozdilek.PM.BuildingBlocks.Persistence;
using Ozdilek.PM.UserDirectoryService.Domain;

namespace Ozdilek.PM.UserDirectoryService.Infrastructure.Persistence;

public class UserDirectoryDbContext(DbContextOptions<UserDirectoryDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("departments");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Name).HasMaxLength(200).IsRequired();
            entity.Property(d => d.IsActive).HasDefaultValue(true);
            entity.HasIndex(d => d.Name).IsUnique();
            entity.HasIndex(d => d.HeadEmployeeId)
                .IsUnique()
                .HasFilter("\"HeadEmployeeId\" IS NOT NULL");
            entity.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(d => d.HeadEmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("employees");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property<string>("RolesCsv").HasColumnName("roles_csv").HasMaxLength(300);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.DepartmentId);
            entity.HasOne<Department>()
                .WithMany()
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.ConfigureBaseEntityKeys();
        base.OnModelCreating(modelBuilder);
        SeedDirectory(modelBuilder);
    }

    private static void SeedDirectory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>().HasData(
            DepartmentSeed.Create("22222222-2222-2222-2222-222222222201", "Arge Proje Müdürlüğü", "11111111-1111-1111-1111-111111111101"),
            DepartmentSeed.Create("22222222-2222-2222-2222-222222222202", "Proje Yönetimi", "11111111-1111-1111-1111-111111111102"),
            DepartmentSeed.Create("22222222-2222-2222-2222-222222222203", "BT Müdürlüğü", "11111111-1111-1111-1111-111111111103"),
            DepartmentSeed.Create("22222222-2222-2222-2222-222222222204", "Teknik Müdürlük", "11111111-1111-1111-1111-111111111104"),
            // Legacy duplicate retained as an archived record so historical project references remain resolvable.
            DepartmentSeed.Create("22222222-2222-2222-2222-222222222205", "BT Departmanı", null, isActive: false),
            DepartmentSeed.Create("22222222-2222-2222-2222-222222222206", "E Ticaret Departmanı", "11111111-1111-1111-1111-111111111106"),
            // Heads are assigned by the normalization migration after both sides of the circular
            // Department <-> Employee relationship have been inserted.
            DepartmentSeed.Create("22222222-2222-2222-2222-222222222207", "Hukuk Departmanı", null),
            DepartmentSeed.Create("22222222-2222-2222-2222-222222222208", "Muhasebe Departmanı", null),
            DepartmentSeed.Create("22222222-2222-2222-2222-222222222209", "Pazarlama Departmanı", null)
        );

        // All seeded accounts share the password "sifre123" (PBKDF2 hash below) — a fixed admin-assigned
        // password per the product decision, not meant to be secure demo data.
        const string seedPasswordHash = "AQAAAAIAAYagAAAAEAx2a0oaK6EvoWj2gBFdMPlHt99WzVLyJyMZqkukRL5JS37Z7R2HEfVwgALpYIr5nQ==";
        // Hash of "admin" — the single dedicated Admin login (product decision: exactly one Admin
        // account, kept separate from any real-named employee so "who is Admin" is unambiguous).
        const string adminPasswordHash = "AQAAAAIAAYagAAAAEJwWJoSAdPaJR1mIqlm7+oh0ZK5GfExiWZ6dpEBvWRw/RAcw8mO2VHEEkcDjgm+YMg==";

        modelBuilder.Entity<Employee>().HasData(
            // Plain employees — no elevated roles. Exactly one Admin and one ProjectManager exist
            // below instead of being scattered across these seed accounts.
            EmployeeSeed.Create("11111111-1111-1111-1111-111111111101", "Ahmet Görür", "ahmet.gorur@example.com", seedPasswordHash, "22222222-2222-2222-2222-222222222201", "Proje Yöneticisi", "Member"),
            EmployeeSeed.Create("11111111-1111-1111-1111-111111111102", "Selin Güler", "selin.guler@example.com", seedPasswordHash, "22222222-2222-2222-2222-222222222202", "Proje Yöneticisi", "Member"),
            EmployeeSeed.Create("11111111-1111-1111-1111-111111111103", "Selim Akar", "selim.akar@example.com", seedPasswordHash, "22222222-2222-2222-2222-222222222203", "Proje Yöneticisi", "Member"),
            EmployeeSeed.Create("11111111-1111-1111-1111-111111111104", "Gamze Demir", "gamze.demir@example.com", seedPasswordHash, "22222222-2222-2222-2222-222222222204", "Birim Sorumlusu", "Member"),
            EmployeeSeed.Create("11111111-1111-1111-1111-111111111105", "Ahmet Gür", "ahmet.gur@example.com", seedPasswordHash, "22222222-2222-2222-2222-222222222203", "BT Uzmanı", "Member"),
            EmployeeSeed.Create("11111111-1111-1111-1111-111111111106", "Merve Tezciler", "merve.tezciler@example.com", seedPasswordHash, "22222222-2222-2222-2222-222222222206", "Birim Sorumlusu", "Member"),
            // The one Admin account — login "admin" / "admin", not a real employee identity.
            EmployeeSeed.Create("11111111-1111-1111-1111-111111111107", "Admin", "admin", adminPasswordHash, null, "Sistem Yöneticisi", "Admin"),
            // The one ProjectManager account.
            EmployeeSeed.Create("11111111-1111-1111-1111-111111111108", "Mustafa Altekin", "mustafa.altekin@example.com", seedPasswordHash, "22222222-2222-2222-2222-222222222202", "Proje Yöneticisi", "ProjectManager"),
            EmployeeSeed.Create("11111111-1111-1111-1111-111111111109", "Zeynep Mutlu", "zeynep.mutlu@example.com", seedPasswordHash, "22222222-2222-2222-2222-222222222207", "Hukuk Uzmanı", "Member"),
            EmployeeSeed.Create("11111111-1111-1111-1111-111111111110", "Elif Edem", "elif.edem@example.com", seedPasswordHash, "22222222-2222-2222-2222-222222222208", "Muhasebe Uzmanı", "Member"),
            EmployeeSeed.Create("11111111-1111-1111-1111-111111111111", "Ece Erenli", "ece.erenli@example.com", seedPasswordHash, "22222222-2222-2222-2222-222222222209", "Pazarlama Uzmanı", "Member"),
            EmployeeSeed.Create("11111111-1111-1111-1111-111111111112", "Onur Yalçın", "onur.yalcin@example.com", seedPasswordHash, "22222222-2222-2222-2222-222222222201", "Proje Planlama Uzmanı", "Member"),
            EmployeeSeed.Create("11111111-1111-1111-1111-111111111113", "Derya Satı", "derya.sati@example.com", seedPasswordHash, "22222222-2222-2222-2222-222222222201", "Maliyet Analisti", "Member"),
            EmployeeSeed.Create("11111111-1111-1111-1111-111111111114", "Yasin Ters", "yasin.ters@example.com", seedPasswordHash, "22222222-2222-2222-2222-222222222202", "Kıdemli Proje Uzmanı", "Member"),
            EmployeeSeed.Create("11111111-1111-1111-1111-111111111115", "Zeynel Mutlu", "zeynel.mutlu@example.com", seedPasswordHash, "22222222-2222-2222-2222-222222222202", "Proje Uzmanı", "Member"),
            EmployeeSeed.Create("11111111-1111-1111-1111-111111111116", "Osman Fır", "osman.fir@example.com", seedPasswordHash, "22222222-2222-2222-2222-222222222203", "Yazılım Uzmanı", "Member"),
            EmployeeSeed.Create("11111111-1111-1111-1111-111111111117", "Ali Eker", "ali.eker@example.com", seedPasswordHash, "22222222-2222-2222-2222-222222222203", "Sistem Uzmanı", "Member"),
            EmployeeSeed.Create("11111111-1111-1111-1111-111111111118", "Defne Satlı", "defne.satli@example.com", seedPasswordHash, "22222222-2222-2222-2222-222222222204", "Teknik Satın Alma Uzmanı", "Member"),
            EmployeeSeed.Create("11111111-1111-1111-1111-111111111119", "Erkan Akacı", "erkan.akaci@example.com", seedPasswordHash, "22222222-2222-2222-2222-222222222204", "Teknik Uzman", "Member"),
            EmployeeSeed.Create("11111111-1111-1111-1111-111111111120", "Deniz Korkmaz", "deniz.korkmaz@example.com", seedPasswordHash, "22222222-2222-2222-2222-222222222206", "E-Ticaret Uzmanı", "Member"),
            EmployeeSeed.Create("11111111-1111-1111-1111-111111111121", "Burcu Aydın", "burcu.aydin@example.com", seedPasswordHash, "22222222-2222-2222-2222-222222222206", "Dijital Operasyon Uzmanı", "Member"),
            EmployeeSeed.Create("11111111-1111-1111-1111-111111111122", "Selin Akar", "selin.akar@example.com", seedPasswordHash, "22222222-2222-2222-2222-222222222207", "Hukuk Müşaviri", "Member"),
            EmployeeSeed.Create("11111111-1111-1111-1111-111111111123", "Ceren Kaya", "ceren.kaya@example.com", seedPasswordHash, "22222222-2222-2222-2222-222222222207", "Sözleşme Uzmanı", "Member"),
            EmployeeSeed.Create("11111111-1111-1111-1111-111111111124", "Gizem Topcu", "gizem.topcu@example.com", seedPasswordHash, "22222222-2222-2222-2222-222222222208", "Muhasebe Uzmanı", "Member"),
            EmployeeSeed.Create("11111111-1111-1111-1111-111111111125", "Eylül Arslan", "eylul.arslan@example.com", seedPasswordHash, "22222222-2222-2222-2222-222222222208", "Bütçe ve Raporlama Uzmanı", "Member"),
            EmployeeSeed.Create("11111111-1111-1111-1111-111111111126", "Zeynep Paslı", "zeynep.pasli@example.com", seedPasswordHash, "22222222-2222-2222-2222-222222222209", "Marka Uzmanı", "Member"),
            EmployeeSeed.Create("11111111-1111-1111-1111-111111111127", "Elif Ekinci", "elif.ekinci@example.com", seedPasswordHash, "22222222-2222-2222-2222-222222222209", "Pazarlama Uzmanı", "Member")
        );
    }
}

/// <summary>Anonymous-object shape EF's HasData seeding needs for Department.</summary>
internal static class DepartmentSeed
{
    public static object Create(string id, string name, string? headEmployeeId, bool isActive = true) => new
    {
        Id = Guid.Parse(id),
        Name = name,
        HeadEmployeeId = headEmployeeId is null ? (Guid?)null : Guid.Parse(headEmployeeId),
        IsActive = isActive,
        CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        UpdatedAtUtc = (DateTimeOffset?)null
    };
}

/// <summary>Anonymous-object shape EF's HasData seeding needs for Employee (property names must match the entity's mapped members, including the private RolesCsv shadow-style property).</summary>
internal static class EmployeeSeed
{
    public static object Create(string id, string displayName, string email, string passwordHash, string? departmentId, string title, string rolesCsv) => new
    {
        Id = Guid.Parse(id),
        DisplayName = displayName,
        Email = email,
        PasswordHash = passwordHash,
        DepartmentId = departmentId is null ? (Guid?)null : Guid.Parse(departmentId),
        IsActive = true,
        CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        UpdatedAtUtc = (DateTimeOffset?)null,
        RolesCsv = rolesCsv,
        Title = title
    };
}
