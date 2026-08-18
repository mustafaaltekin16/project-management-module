using FluentAssertions;
using Ozdilek.PM.AIGatewayService.Application.Dtos;
using Ozdilek.PM.AIGatewayService.Application.Services;
using Xunit;

namespace Ozdilek.PM.AIGatewayService.Tests;

public class SyntheticContextDocumentFormatterTests
{
    [Fact]
    public void FormatExistingTasks_WithNoTasks_ReturnsEmptyString()
    {
        var result = SyntheticContextDocumentFormatter.FormatExistingTasks([]);

        result.Should().BeEmpty();
    }

    [Fact]
    public void FormatExistingTasks_WithTasks_NumbersThemAndIncludesStatusDateRangeAndDescription()
    {
        var existingTasks = new[]
        {
            new ExistingTaskInfoDto("Zemin etüdü ve proje onayları", "Zemin analiz raporu ve onay süreci.", "Done",
                new DateTimeOffset(2024, 3, 4, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2024, 3, 22, 0, 0, 0, TimeSpan.Zero)),
            new ExistingTaskInfoDto("İskele kurulumu ve İSG onayı", null, "InProgress", null, null)
        };

        var result = SyntheticContextDocumentFormatter.FormatExistingTasks(existingTasks);

        result.Should().Contain("1. \"Zemin etüdü ve proje onayları\" — durum: Done");
        result.Should().Contain("04.03.2024").And.Contain("22.03.2024");
        result.Should().Contain("Açıklama: Zemin analiz raporu ve onay süreci.");
        result.Should().Contain("2. \"İskele kurulumu ve İSG onayı\" — durum: InProgress, tarih planlanmamış");
    }

    [Fact]
    public void FormatPendingSuggestionTitles_WithNoTitles_ReturnsEmptyString()
    {
        var result = SyntheticContextDocumentFormatter.FormatPendingSuggestionTitles([]);

        result.Should().BeEmpty();
    }

    [Fact]
    public void FormatPendingSuggestionTitles_WithTitles_ListsEachAsQuotedBullet()
    {
        var result = SyntheticContextDocumentFormatter.FormatPendingSuggestionTitles(["İzin ve Ruhsat Süreçleri", "Zemin Etüdü"]);

        result.Should().Contain("- \"İzin ve Ruhsat Süreçleri\"");
        result.Should().Contain("- \"Zemin Etüdü\"");
    }
}
