using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Ozdilek.PM.UserDirectoryService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeDirectoryAndSeedDepartmentUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Release head assignments before normalizing the development directory. A person can
            // head only one department and users may have changed the seeded assignments through
            // the management screen before this migration is applied.
            migrationBuilder.Sql(
                """
                UPDATE departments
                SET "HeadEmployeeId" = NULL,
                    "UpdatedAtUtc" = NOW()
                WHERE "HeadEmployeeId" IN (
                    '11111111-1111-1111-1111-111111111101',
                    '11111111-1111-1111-1111-111111111102',
                    '11111111-1111-1111-1111-111111111103',
                    '11111111-1111-1111-1111-111111111104',
                    '11111111-1111-1111-1111-111111111106'
                );

                UPDATE departments
                SET "Name" = LEFT("Name", 160) || ' (Arşiv ' || LEFT("Id"::text, 8) || ')',
                    "HeadEmployeeId" = NULL,
                    "IsActive" = FALSE,
                    "UpdatedAtUtc" = NOW()
                WHERE "Id" NOT IN (
                    '22222222-2222-2222-2222-222222222207',
                    '22222222-2222-2222-2222-222222222208',
                    '22222222-2222-2222-2222-222222222209'
                )
                AND "Name" IN (
                    'Hukuk Departmani',
                    'Hukuk Departmanı',
                    'Muhasebe Departmani',
                    'Muhasebe Departmanı',
                    'Pazarlama Departmani',
                    'Pazarlama Departmanı'
                );

                UPDATE departments
                SET "Name" = 'Test Departmanı (Arşiv ' || LEFT("Id"::text, 8) || ')',
                    "HeadEmployeeId" = NULL,
                    "IsActive" = FALSE,
                    "UpdatedAtUtc" = NOW()
                WHERE "Name" IN ('Test Departmani', 'Test Departmanı');

                UPDATE departments
                SET "Name" = 'Arge (Arşiv ' || LEFT("Id"::text, 8) || ')',
                    "HeadEmployeeId" = NULL,
                    "IsActive" = FALSE,
                    "UpdatedAtUtc" = NOW()
                WHERE "Id" <> '22222222-2222-2222-2222-222222222201'
                  AND LOWER("Name") = 'arge';
                """);

            migrationBuilder.UpdateData(
                table: "departments",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222205"),
                column: "IsActive",
                value: false);

            migrationBuilder.UpdateData(
                table: "departments",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222206"),
                column: "HeadEmployeeId",
                value: new Guid("11111111-1111-1111-1111-111111111106"));

            migrationBuilder.InsertData(
                table: "departments",
                columns: new[] { "Id", "CreatedAtUtc", "HeadEmployeeId", "IsActive", "Name", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222207"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Hukuk Departmanı", null },
                    { new Guid("22222222-2222-2222-2222-222222222208"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Muhasebe Departmanı", null },
                    { new Guid("22222222-2222-2222-2222-222222222209"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Pazarlama Departmanı", null }
                });

            migrationBuilder.UpdateData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                columns: new[] { "DepartmentId", "Title" },
                values: new object[] { new Guid("22222222-2222-2222-2222-222222222203"), "BT Uzmanı" });

            migrationBuilder.InsertData(
                table: "employees",
                columns: new[] { "Id", "CreatedAtUtc", "DepartmentId", "DisplayName", "Email", "IsActive", "PasswordHash", "roles_csv", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111109"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222207"), "Zeynep Mutlu", "zeynep.mutlu@example.com", true, "AQAAAAIAAYagAAAAEAx2a0oaK6EvoWj2gBFdMPlHt99WzVLyJyMZqkukRL5JS37Z7R2HEfVwgALpYIr5nQ==", "Member", "Hukuk Uzmanı", null },
                    { new Guid("11111111-1111-1111-1111-111111111110"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222208"), "Elif Edem", "elif.edem@example.com", true, "AQAAAAIAAYagAAAAEAx2a0oaK6EvoWj2gBFdMPlHt99WzVLyJyMZqkukRL5JS37Z7R2HEfVwgALpYIr5nQ==", "Member", "Muhasebe Uzmanı", null },
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222209"), "Ece Erenli", "ece.erenli@example.com", true, "AQAAAAIAAYagAAAAEAx2a0oaK6EvoWj2gBFdMPlHt99WzVLyJyMZqkukRL5JS37Z7R2HEfVwgALpYIr5nQ==", "Member", "Pazarlama Uzmanı", null }
                });

            // Reconcile records that were edited or added through the directory screen before this
            // migration. The legacy BT department is archived, but its people remain active under
            // the canonical BT Müdürlüğü record.
            migrationBuilder.Sql(
                """
                UPDATE employees
                SET "DepartmentId" = '22222222-2222-2222-2222-222222222203',
                    "UpdatedAtUtc" = NOW()
                WHERE "DepartmentId" = '22222222-2222-2222-2222-222222222205';

                UPDATE employees
                SET "DepartmentId" = CASE "Id"
                        WHEN '11111111-1111-1111-1111-111111111101' THEN '22222222-2222-2222-2222-222222222201'::uuid
                        WHEN '11111111-1111-1111-1111-111111111102' THEN '22222222-2222-2222-2222-222222222202'::uuid
                        WHEN '11111111-1111-1111-1111-111111111103' THEN '22222222-2222-2222-2222-222222222203'::uuid
                        WHEN '11111111-1111-1111-1111-111111111104' THEN '22222222-2222-2222-2222-222222222204'::uuid
                        WHEN '11111111-1111-1111-1111-111111111106' THEN '22222222-2222-2222-2222-222222222206'::uuid
                        WHEN '11111111-1111-1111-1111-111111111108' THEN '22222222-2222-2222-2222-222222222202'::uuid
                    END,
                    "UpdatedAtUtc" = NOW()
                WHERE "Id" IN (
                    '11111111-1111-1111-1111-111111111101',
                    '11111111-1111-1111-1111-111111111102',
                    '11111111-1111-1111-1111-111111111103',
                    '11111111-1111-1111-1111-111111111104',
                    '11111111-1111-1111-1111-111111111106',
                    '11111111-1111-1111-1111-111111111108'
                );

                UPDATE employees
                SET "DepartmentId" = NULL,
                    "IsActive" = FALSE,
                    "UpdatedAtUtc" = NOW()
                WHERE LOWER("Email") IN ('test.kullanici@example.com', 'test@example.com')
                   OR LOWER("DisplayName") IN ('test kullanici', 'test kullanıcı');

                UPDATE departments
                SET "HeadEmployeeId" = CASE "Id"
                        WHEN '22222222-2222-2222-2222-222222222201' THEN '11111111-1111-1111-1111-111111111101'::uuid
                        WHEN '22222222-2222-2222-2222-222222222202' THEN '11111111-1111-1111-1111-111111111102'::uuid
                        WHEN '22222222-2222-2222-2222-222222222203' THEN '11111111-1111-1111-1111-111111111103'::uuid
                        WHEN '22222222-2222-2222-2222-222222222204' THEN '11111111-1111-1111-1111-111111111104'::uuid
                        WHEN '22222222-2222-2222-2222-222222222206' THEN '11111111-1111-1111-1111-111111111106'::uuid
                        WHEN '22222222-2222-2222-2222-222222222207' THEN '11111111-1111-1111-1111-111111111109'::uuid
                        WHEN '22222222-2222-2222-2222-222222222208' THEN '11111111-1111-1111-1111-111111111110'::uuid
                        WHEN '22222222-2222-2222-2222-222222222209' THEN '11111111-1111-1111-1111-111111111111'::uuid
                    END,
                    "UpdatedAtUtc" = NOW()
                WHERE "Id" IN (
                    '22222222-2222-2222-2222-222222222201',
                    '22222222-2222-2222-2222-222222222202',
                    '22222222-2222-2222-2222-222222222203',
                    '22222222-2222-2222-2222-222222222204',
                    '22222222-2222-2222-2222-222222222206',
                    '22222222-2222-2222-2222-222222222207',
                    '22222222-2222-2222-2222-222222222208',
                    '22222222-2222-2222-2222-222222222209'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE departments
                SET "HeadEmployeeId" = NULL
                WHERE "HeadEmployeeId" IN (
                    '11111111-1111-1111-1111-111111111109',
                    '11111111-1111-1111-1111-111111111110',
                    '11111111-1111-1111-1111-111111111111'
                );
                """);

            migrationBuilder.DeleteData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111109"));

            migrationBuilder.DeleteData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111110"));

            migrationBuilder.DeleteData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "departments",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222207"));

            migrationBuilder.DeleteData(
                table: "departments",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222208"));

            migrationBuilder.DeleteData(
                table: "departments",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222209"));

            migrationBuilder.UpdateData(
                table: "departments",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222206"),
                column: "HeadEmployeeId",
                value: null);

            migrationBuilder.UpdateData(
                table: "departments",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222205"),
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                columns: new[] { "DepartmentId", "Title" },
                values: new object[] { new Guid("22222222-2222-2222-2222-222222222205"), "Birim Sorumlusu" });
        }
    }
}
