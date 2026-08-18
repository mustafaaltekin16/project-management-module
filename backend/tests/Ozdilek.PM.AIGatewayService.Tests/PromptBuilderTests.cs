using FluentAssertions;
using Ozdilek.PM.AIGatewayService.Application.Dtos;
using Ozdilek.PM.AIGatewayService.Application.Services;
using Xunit;

namespace Ozdilek.PM.AIGatewayService.Tests;

public class PromptBuilderTests
{
    [Fact]
    public void Build_SubstitutesAllPlaceholders()
    {
        var template = PromptBuilder.DefaultTemplateFor("FeasibilityBased");
        var project = new ProjectInfoDto(Guid.NewGuid(), "Ev Tekstil Mağaza Açılışı", "Yeni mağaza açılışı fizibilitesi", "FeasibilityBased", "Arge Proje Müdürlüğü", []);

        var prompt = PromptBuilder.Build(template, project, "Teknik standartlara uy.");

        prompt.Should().Contain("Ev Tekstil Mağaza Açılışı");
        prompt.Should().Contain("Yeni mağaza açılışı fizibilitesi");
        prompt.Should().Contain("FeasibilityBased");
        prompt.Should().Contain("Arge Proje Müdürlüğü");
        prompt.Should().Contain("Teknik standartlara uy.");
        prompt.Should().NotContain("{ProjectName}");
    }

    [Fact]
    public void Build_WithoutExtraInstructions_UsesPlaceholderText()
    {
        var template = PromptBuilder.DefaultTemplateFor("Simple");
        var project = new ProjectInfoDto(Guid.NewGuid(), "Ocak Seti Kurulması", "açıklama", "Simple", "Şafabat Lokantası", []);

        var prompt = PromptBuilder.Build(template, project, null);

        prompt.Should().Contain("(yok)");
    }

    [Fact]
    public void AppendDocumentExcerpts_WithNoExcerpts_ReturnsPromptUnchanged()
    {
        var result = PromptBuilder.AppendDocumentExcerpts("orijinal prompt", []);

        result.Should().Be("orijinal prompt");
    }

    [Fact]
    public void AppendDocumentExcerpts_WithOneDocument_AppendsNameAndText()
    {
        var excerpts = new[] { new DocumentExcerpt("FizibiliteRaporu.pdf", "rapor içeriği") };

        var result = PromptBuilder.AppendDocumentExcerpts("orijinal prompt", excerpts);

        result.Should().Contain("orijinal prompt");
        result.Should().Contain("FizibiliteRaporu.pdf");
        result.Should().Contain("rapor içeriği");
    }

    [Fact]
    public void AppendDocumentExcerpts_WithMultipleDocuments_IncludesAllOfThem()
    {
        var excerpts = new[]
        {
            new DocumentExcerpt("A.pdf", "A içeriği"),
            new DocumentExcerpt("B.docx", "B içeriği")
        };

        var result = PromptBuilder.AppendDocumentExcerpts("prompt", excerpts);

        result.Should().Contain("A.pdf").And.Contain("A içeriği");
        result.Should().Contain("B.docx").And.Contain("B içeriği");
    }

    [Fact]
    public void AppendDepartmentList_WithNoDepartments_ReturnsPromptUnchanged()
    {
        var result = PromptBuilder.AppendDepartmentList("orijinal prompt", []);

        result.Should().Be("orijinal prompt");
    }

    [Fact]
    public void AppendDepartmentList_WithDepartments_ListsRealTitlesAndForbidsInventingNewOnes()
    {
        var departments = new[]
        {
            new ProjectDepartmentInfoDto("Zemin Etüdü ve Proje Onayları", "Arge Proje Müdürlüğü"),
            new ProjectDepartmentInfoDto("Ruhsat, İzin ve Yasal Süreçler", "Hukuk Departmanı")
        };

        var result = PromptBuilder.AppendDepartmentList("prompt", departments);

        result.Should().Contain("Zemin Etüdü ve Proje Onayları").And.Contain("Arge Proje Müdürlüğü");
        result.Should().Contain("Ruhsat, İzin ve Yasal Süreçler").And.Contain("Hukuk Departmanı");
        result.Should().Contain("UYDURMA");
    }

    [Fact]
    public void AppendExistingTasksList_WithNoTasks_ReturnsPromptUnchanged()
    {
        var result = PromptBuilder.AppendExistingTasksList("orijinal prompt", []);

        result.Should().Be("orijinal prompt");
    }

    [Fact]
    public void AppendExistingTasksList_WithRetrievedContexts_IncludesThemAndForbidsDuplicatingThem()
    {
        var retrievedContexts = new[]
        {
            "1. \"Zemin etüdü ve proje onayları\" — durum: Done, 04.03.2024 – 22.03.2024\n   Açıklama: Zemin analiz raporu ve onay süreci.",
            "2. \"İskele kurulumu ve İSG onayı\" — durum: InProgress, tarih planlanmamış"
        };

        var result = PromptBuilder.AppendExistingTasksList("prompt", retrievedContexts);

        result.Should().Contain("Zemin etüdü ve proje onayları").And.Contain("Zemin analiz raporu ve onay süreci.");
        result.Should().Contain("İskele kurulumu ve İSG onayı");
        result.Should().Contain("TEKRAR önerme");
        result.Should().Contain("sequenceNote");
    }

    [Fact]
    public void AppendExistingTasksList_WithRetrievedContexts_InstructsGapDetectionBetweenConsecutiveTasks()
    {
        var result = PromptBuilder.AppendExistingTasksList("prompt", ["1. \"Zemin etüdü\" — durum: Done, tarih planlanmamış"]);

        result.Should().Contain("ART ARDA gelen iki görev");
        result.Should().Contain("ATLANMIŞ");
    }

    [Fact]
    public void AppendExistingTasksList_WithRetrievedContexts_InstructsSequenceRankForSiblingOrdering()
    {
        var result = PromptBuilder.AppendExistingTasksList("prompt", ["1. \"Zemin etüdü\" — durum: Done, tarih planlanmamış"]);

        result.Should().Contain("sequenceRank");
        result.Should().Contain("kardeş bir önerinin başlığına");
    }

    [Fact]
    public void AppendExistingTasksList_WithRetrievedContexts_InstructsIsAtProjectStartForNullAnchor()
    {
        var result = PromptBuilder.AppendExistingTasksList("prompt", ["1. \"Zemin etüdü\" — durum: Done, tarih planlanmamış"]);

        result.Should().Contain("isAtProjectStart");
        result.Should().Contain("EN SONUNDAYMIŞ gibi gösterilir");
    }

    [Fact]
    public void AppendPendingSuggestionTitles_WithNoTitles_ReturnsPromptUnchanged()
    {
        var result = PromptBuilder.AppendPendingSuggestionTitles("orijinal prompt", []);

        result.Should().Be("orijinal prompt");
    }

    [Fact]
    public void AppendPendingSuggestionTitles_WithTitles_ListsThemAndForbidsRepeatingThem()
    {
        var result = PromptBuilder.AppendPendingSuggestionTitles("prompt", ["İzin ve Ruhsat Süreçleri", "Zemin Etüdü"]);

        result.Should().Contain("İzin ve Ruhsat Süreçleri").And.Contain("Zemin Etüdü");
        result.Should().Contain("BİREBİR TEKRAR üretme");
    }
}
