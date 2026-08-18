using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Ozdilek.PM.UserDirectoryService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandDepartmentEmployeeDirectory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "employees",
                columns: new[] { "Id", "CreatedAtUtc", "DepartmentId", "DisplayName", "Email", "IsActive", "PasswordHash", "roles_csv", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111112"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222201"), "Onur Yalçın", "onur.yalcin@example.com", true, "AQAAAAIAAYagAAAAEAx2a0oaK6EvoWj2gBFdMPlHt99WzVLyJyMZqkukRL5JS37Z7R2HEfVwgALpYIr5nQ==", "Member", "Proje Planlama Uzmanı", null },
                    { new Guid("11111111-1111-1111-1111-111111111113"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222201"), "Derya Satı", "derya.sati@example.com", true, "AQAAAAIAAYagAAAAEAx2a0oaK6EvoWj2gBFdMPlHt99WzVLyJyMZqkukRL5JS37Z7R2HEfVwgALpYIr5nQ==", "Member", "Maliyet Analisti", null },
                    { new Guid("11111111-1111-1111-1111-111111111114"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222202"), "Yasin Ters", "yasin.ters@example.com", true, "AQAAAAIAAYagAAAAEAx2a0oaK6EvoWj2gBFdMPlHt99WzVLyJyMZqkukRL5JS37Z7R2HEfVwgALpYIr5nQ==", "Member", "Kıdemli Proje Uzmanı", null },
                    { new Guid("11111111-1111-1111-1111-111111111115"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222202"), "Zeynel Mutlu", "zeynel.mutlu@example.com", true, "AQAAAAIAAYagAAAAEAx2a0oaK6EvoWj2gBFdMPlHt99WzVLyJyMZqkukRL5JS37Z7R2HEfVwgALpYIr5nQ==", "Member", "Proje Uzmanı", null },
                    { new Guid("11111111-1111-1111-1111-111111111116"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222203"), "Osman Fır", "osman.fir@example.com", true, "AQAAAAIAAYagAAAAEAx2a0oaK6EvoWj2gBFdMPlHt99WzVLyJyMZqkukRL5JS37Z7R2HEfVwgALpYIr5nQ==", "Member", "Yazılım Uzmanı", null },
                    { new Guid("11111111-1111-1111-1111-111111111117"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222203"), "Ali Eker", "ali.eker@example.com", true, "AQAAAAIAAYagAAAAEAx2a0oaK6EvoWj2gBFdMPlHt99WzVLyJyMZqkukRL5JS37Z7R2HEfVwgALpYIr5nQ==", "Member", "Sistem Uzmanı", null },
                    { new Guid("11111111-1111-1111-1111-111111111118"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222204"), "Defne Satlı", "defne.satli@example.com", true, "AQAAAAIAAYagAAAAEAx2a0oaK6EvoWj2gBFdMPlHt99WzVLyJyMZqkukRL5JS37Z7R2HEfVwgALpYIr5nQ==", "Member", "Teknik Satın Alma Uzmanı", null },
                    { new Guid("11111111-1111-1111-1111-111111111119"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222204"), "Erkan Akacı", "erkan.akaci@example.com", true, "AQAAAAIAAYagAAAAEAx2a0oaK6EvoWj2gBFdMPlHt99WzVLyJyMZqkukRL5JS37Z7R2HEfVwgALpYIr5nQ==", "Member", "Teknik Uzman", null },
                    { new Guid("11111111-1111-1111-1111-111111111120"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222206"), "Deniz Korkmaz", "deniz.korkmaz@example.com", true, "AQAAAAIAAYagAAAAEAx2a0oaK6EvoWj2gBFdMPlHt99WzVLyJyMZqkukRL5JS37Z7R2HEfVwgALpYIr5nQ==", "Member", "E-Ticaret Uzmanı", null },
                    { new Guid("11111111-1111-1111-1111-111111111121"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222206"), "Burcu Aydın", "burcu.aydin@example.com", true, "AQAAAAIAAYagAAAAEAx2a0oaK6EvoWj2gBFdMPlHt99WzVLyJyMZqkukRL5JS37Z7R2HEfVwgALpYIr5nQ==", "Member", "Dijital Operasyon Uzmanı", null },
                    { new Guid("11111111-1111-1111-1111-111111111122"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222207"), "Selin Akar", "selin.akar@example.com", true, "AQAAAAIAAYagAAAAEAx2a0oaK6EvoWj2gBFdMPlHt99WzVLyJyMZqkukRL5JS37Z7R2HEfVwgALpYIr5nQ==", "Member", "Hukuk Müşaviri", null },
                    { new Guid("11111111-1111-1111-1111-111111111123"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222207"), "Ceren Kaya", "ceren.kaya@example.com", true, "AQAAAAIAAYagAAAAEAx2a0oaK6EvoWj2gBFdMPlHt99WzVLyJyMZqkukRL5JS37Z7R2HEfVwgALpYIr5nQ==", "Member", "Sözleşme Uzmanı", null },
                    { new Guid("11111111-1111-1111-1111-111111111124"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222208"), "Gizem Topcu", "gizem.topcu@example.com", true, "AQAAAAIAAYagAAAAEAx2a0oaK6EvoWj2gBFdMPlHt99WzVLyJyMZqkukRL5JS37Z7R2HEfVwgALpYIr5nQ==", "Member", "Muhasebe Uzmanı", null },
                    { new Guid("11111111-1111-1111-1111-111111111125"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222208"), "Eylül Arslan", "eylul.arslan@example.com", true, "AQAAAAIAAYagAAAAEAx2a0oaK6EvoWj2gBFdMPlHt99WzVLyJyMZqkukRL5JS37Z7R2HEfVwgALpYIr5nQ==", "Member", "Bütçe ve Raporlama Uzmanı", null },
                    { new Guid("11111111-1111-1111-1111-111111111126"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222209"), "Zeynep Paslı", "zeynep.pasli@example.com", true, "AQAAAAIAAYagAAAAEAx2a0oaK6EvoWj2gBFdMPlHt99WzVLyJyMZqkukRL5JS37Z7R2HEfVwgALpYIr5nQ==", "Member", "Marka Uzmanı", null },
                    { new Guid("11111111-1111-1111-1111-111111111127"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("22222222-2222-2222-2222-222222222209"), "Elif Ekinci", "elif.ekinci@example.com", true, "AQAAAAIAAYagAAAAEAx2a0oaK6EvoWj2gBFdMPlHt99WzVLyJyMZqkukRL5JS37Z7R2HEfVwgALpYIr5nQ==", "Member", "Pazarlama Uzmanı", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111112"));

            migrationBuilder.DeleteData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111113"));

            migrationBuilder.DeleteData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111114"));

            migrationBuilder.DeleteData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111115"));

            migrationBuilder.DeleteData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111116"));

            migrationBuilder.DeleteData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111117"));

            migrationBuilder.DeleteData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111118"));

            migrationBuilder.DeleteData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111119"));

            migrationBuilder.DeleteData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111120"));

            migrationBuilder.DeleteData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111121"));

            migrationBuilder.DeleteData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111122"));

            migrationBuilder.DeleteData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111123"));

            migrationBuilder.DeleteData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111124"));

            migrationBuilder.DeleteData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111125"));

            migrationBuilder.DeleteData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111126"));

            migrationBuilder.DeleteData(
                table: "employees",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111127"));
        }
    }
}
