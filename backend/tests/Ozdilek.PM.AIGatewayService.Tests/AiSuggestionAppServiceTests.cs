using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ozdilek.PM.AIGatewayService.Application.Dtos;
using Ozdilek.PM.AIGatewayService.Application.Interfaces;
using Ozdilek.PM.AIGatewayService.Application.Services;
using Ozdilek.PM.AIGatewayService.Domain;
using Ozdilek.PM.SharedKernel.Events;
using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;
using Xunit;

namespace Ozdilek.PM.AIGatewayService.Tests;

public class AiSuggestionAppServiceTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly ProjectInfoDto Project = new(
        ProjectId, "Test Projesi", "Açıklama", "Yazılım", "Birim",
        [new ProjectDepartmentInfoDto("Analiz", "Analiz")]);

    private const string EmptySuggestionsJson = "[]";

    private sealed class Fixture
    {
        public Mock<IAiSuggestionRequestRepository> Requests { get; } = new();
        public Mock<IPromptTemplateRepository> Templates { get; } = new();
        public Mock<IProjectInfoClient> ProjectInfoClient { get; } = new();
        public Mock<IRagDocumentSyncService> RagDocumentSyncService { get; } = new();
        public Mock<IRagClient> RagClient { get; } = new();
        public Mock<ITaskInfoClient> TaskInfoClient { get; } = new();
        public Mock<IWorkPackageContextRetrievalService> WorkPackageContextRetrievalService { get; } = new();
        public Mock<ILlmProvider> LlmProvider { get; } = new();
        public Mock<IPromptAuditLogger> AuditLogger { get; } = new();
        public Mock<IEventPublisher> EventPublisher { get; } = new();
        public Mock<IUnitOfWork> UnitOfWork { get; } = new();

        public Fixture()
        {
            ProjectInfoClient.Setup(c => c.GetProjectAsync(ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync(Project);
            Templates.Setup(t => t.GetByProjectTypeAsync(Project.Type, It.IsAny<CancellationToken>())).ReturnsAsync((PromptTemplate?)null);
            TaskInfoClient.Setup(c => c.ListExistingTasksAsync(ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
            Requests.Setup(r => r.ListByProjectAsync(ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
            WorkPackageContextRetrievalService
                .Setup(s => s.RetrieveExistingTaskContextAsync(
                    It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ExistingTaskInfoDto>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<string>)[]);
            WorkPackageContextRetrievalService
                .Setup(s => s.RetrievePendingSuggestionContextAsync(
                    It.IsAny<Guid>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<string>)[]);
            LlmProvider.SetupGet(p => p.Name).Returns("Mock");
            LlmProvider.Setup(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmptySuggestionsJson);
        }

        public AiSuggestionAppService CreateService(
            RagOptions? ragOptions = null, DocumentExcerptOptions? excerptOptions = null,
            WorkPackageContextRetrievalOptions? workPackageContextRetrievalOptions = null) => new(
            Requests.Object, Templates.Object, ProjectInfoClient.Object, RagDocumentSyncService.Object, RagClient.Object,
            ragOptions ?? new RagOptions(), TaskInfoClient.Object, WorkPackageContextRetrievalService.Object,
            workPackageContextRetrievalOptions ?? new WorkPackageContextRetrievalOptions(),
            excerptOptions ?? new DocumentExcerptOptions(),
            LlmProvider.Object, AuditLogger.Object, EventPublisher.Object, UnitOfWork.Object,
            new WorkPackageGenerationLockRegistry(), NullLogger<AiSuggestionAppService>.Instance);
    }

    [Fact]
    public async Task GenerateAsync_NoSelectedDocumentIds_MakesNoRagCalls()
    {
        var fixture = new Fixture();
        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: null);

        var result = await service.GenerateAsync(request);

        fixture.RagDocumentSyncService.Verify(
            s => s.EnsureProjectDocumentsSyncedAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        fixture.RagClient.Verify(c => c.AskAsync(It.IsAny<RagAskRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        result.UsedRealDocumentContext.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateAsync_EmptySelectedDocumentIds_MakesNoRagCalls()
    {
        var fixture = new Fixture();
        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: []);

        await service.GenerateAsync(request);

        fixture.RagDocumentSyncService.Verify(
            s => s.EnsureProjectDocumentsSyncedAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GenerateAsync_RagReturnsEmptyRetrievedContexts_GenerationStillProceedsWithoutExcerpts()
    {
        var fixture = new Fixture();
        var documentId = Guid.NewGuid();
        fixture.RagDocumentSyncService
            .Setup(s => s.EnsureProjectDocumentsSyncedAsync(ProjectId, It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(documentId)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagSyncResult(["belge.pdf"], ["belge.pdf"], true));
        fixture.RagClient
            .Setup(c => c.AskAsync(It.IsAny<RagAskRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagAnswer(true, "ok", "cevap yok", null, null, RetrievedContexts: []));

        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: [documentId]);

        var result = await service.GenerateAsync(request);

        result.Should().NotBeNull();
        fixture.Requests.Verify(r => r.AddAsync(It.IsAny<AiSuggestionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        result.UsedRealDocumentContext.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateAsync_RagReturnsContextsWithSources_ProducesExactlyOneExcerptNamedFromSources()
    {
        var fixture = new Fixture();
        var documentId = Guid.NewGuid();
        string? capturedPrompt = null;
        fixture.RagDocumentSyncService
            .Setup(s => s.EnsureProjectDocumentsSyncedAsync(ProjectId, It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(documentId)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagSyncResult(["belge.pdf"], ["belge.pdf"], true));
        fixture.RagClient
            .Setup(c => c.AskAsync(It.IsAny<RagAskRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagAnswer(true, "ok", "cevap", null, ["belge.pdf"], ["alakalı parça metni"]));
        fixture.AuditLogger
            .Setup(a => a.LogAsync(ProjectId, "Mock", It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, string, string, IReadOnlyList<string>, CancellationToken>((_, _, prompt, _, _) => capturedPrompt = prompt)
            .Returns(Task.CompletedTask);

        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: [documentId]);

        var result = await service.GenerateAsync(request);

        result.SelectedDocumentNames.Should().ContainSingle().Which.Should().Be("belge.pdf");
        capturedPrompt.Should().Contain("alakalı parça metni");
        result.UsedRealDocumentContext.Should().BeTrue();
    }

    [Fact]
    public async Task GenerateAsync_NoExistingTasksAndNoPendingSuggestions_NeverCallsContextRetrievalService()
    {
        var fixture = new Fixture();
        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: null);

        await service.GenerateAsync(request);

        // Existing-task count (0) is under FullListThreshold, so this now bypasses RAG entirely rather
        // than calling it with an empty list — see GenerateAsync_ExistingTasksBelowThreshold_BypassesRag.
        fixture.WorkPackageContextRetrievalService.Verify(
            s => s.RetrieveExistingTaskContextAsync(
                It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ExistingTaskInfoDto>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        fixture.WorkPackageContextRetrievalService.Verify(
            s => s.RetrievePendingSuggestionContextAsync(
                It.IsAny<Guid>(), It.Is<IReadOnlyList<string>>(l => l.Count == 0), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_ExistingTasksBelowThreshold_BypassesRagAndFormatsFullListDirectly()
    {
        var fixture = new Fixture();
        var existingTasks = new List<ExistingTaskInfoDto>
        {
            new("Zemin Etüdü", null, "Done", null, null),
            new("Ruhsat Süreçleri", null, "InProgress", null, null),
        };
        fixture.TaskInfoClient.Setup(c => c.ListExistingTasksAsync(ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync(existingTasks);
        string? capturedPrompt = null;
        fixture.AuditLogger
            .Setup(a => a.LogAsync(ProjectId, "Mock", It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, string, string, IReadOnlyList<string>, CancellationToken>((_, _, prompt, _, _) => capturedPrompt = prompt)
            .Returns(Task.CompletedTask);

        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: null);

        await service.GenerateAsync(request);

        // Small lists (<= FullListThreshold) skip RAG's semantic-subset retrieval altogether — every
        // existing task must reach the prompt directly, not just whatever RAG deemed "relevant".
        fixture.WorkPackageContextRetrievalService.Verify(
            s => s.RetrieveExistingTaskContextAsync(
                It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ExistingTaskInfoDto>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        capturedPrompt.Should().Contain("Zemin Etüdü").And.Contain("Ruhsat Süreçleri");
    }

    [Fact]
    public async Task GenerateAsync_ExistingTasksAboveThreshold_UsesRagSemanticSubsetRetrieval()
    {
        var fixture = new Fixture();
        var existingTasks = Enumerable.Range(1, 21)
            .Select(i => new ExistingTaskInfoDto($"Görev {i}", null, "Done", null, null))
            .ToList();
        fixture.TaskInfoClient.Setup(c => c.ListExistingTasksAsync(ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync(existingTasks);

        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: null);

        await service.GenerateAsync(request);

        fixture.WorkPackageContextRetrievalService.Verify(
            s => s.RetrieveExistingTaskContextAsync(
                ProjectId,
                It.Is<IReadOnlyList<ExistingTaskInfoDto>>(l => l.Count == 21),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_SuggestionTitleDiffersOnlyByPunctuationFromExistingTask_IsFilteredOut()
    {
        var fixture = new Fixture();
        var existingTasks = new List<ExistingTaskInfoDto> { new("UI/UX Tasarım", null, "Done", null, null) };
        fixture.TaskInfoClient.Setup(c => c.ListExistingTasksAsync(ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync(existingTasks);
        fixture.LlmProvider
            .Setup(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""[{"title":"UI UX Tasarım","department":"Analiz","effortHours":10,"sourceDocument":null,"description":null,"sequenceNote":null,"insertAfterTaskTitle":null,"sequenceRank":1,"isAtProjectStart":true,"activities":[]}]""");

        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: null);

        var result = await service.GenerateAsync(request);

        result.Items.Should().BeEmpty(
            "\"UI UX Tasarım\" and \"UI/UX Tasarım\" differ only by punctuation and must be treated as the same task");
    }

    [Fact]
    public async Task GenerateAsync_LlmReturnsBareObjectInsteadOfArray_IsTreatedAsSingleSuggestion()
    {
        // Canlıda gözlenen gerçek bir arıza modu: model istenen [ ] dizi zarfını atlayıp tek bir iş
        // paketi ürettiğinde doğrudan tek bir JSON nesnesi döndürüyor. Bunu DomainException ile
        // reddetmek yerine tek elemanlı bir öneri listesi olarak kabul etmemiz gerekiyor.
        var fixture = new Fixture();
        fixture.LlmProvider
            .Setup(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"title":"Saha Etüdü","department":"Analiz","effortHours":10,"sourceDocument":null,"description":"Saha ölçümleri yapılır.","sequenceNote":null,"insertAfterTaskTitle":null,"sequenceRank":1,"isAtProjectStart":true,"activities":[]}""");

        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: null);

        var result = await service.GenerateAsync(request);

        result.Items.Should().ContainSingle();
        result.Items[0].Title.Should().Be("Saha Etüdü");
    }

    [Fact]
    public async Task GenerateAsync_ArrayContainsAStrayNonObjectElement_ValidSuggestionsSurviveAndTheStrayOneIsSkipped()
    {
        // Canlıda gözlenen ikinci bir arıza modu: model gerçekten bir dizi döndürüyor ama dizinin
        // içine, hiçbir şemaya uymayan başıboş bir string (ör. sadece "task") sıkıştırıyor. Bu tek öğe
        // yüzünden dizideki GEÇERLİ önerilerin de reddedilmemesi gerekiyor.
        var fixture = new Fixture();
        fixture.LlmProvider
            .Setup(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                [{"title":"Saha Etüdü","department":"Analiz","effortHours":10,"sourceDocument":null,"description":null,"sequenceNote":null,"insertAfterTaskTitle":null,"sequenceRank":1,"isAtProjectStart":true,"activities":[]},
                 "task",
                 {"title":"Zemin Testi","department":"Analiz","effortHours":8,"sourceDocument":null,"description":null,"sequenceNote":null,"insertAfterTaskTitle":null,"sequenceRank":2,"isAtProjectStart":true,"activities":[]}]
                """);

        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: null);

        var result = await service.GenerateAsync(request);

        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.Title).Should().BeEquivalentTo(["Saha Etüdü", "Zemin Testi"]);
    }

    [Fact]
    public async Task GenerateAsync_ResponseIsSyntacticallyInvalidJson_RecoversValidObjectsFromTheText()
    {
        // Canlıda gözlenen üçüncü bir arıza modu: model dizinin içine süslü parantezsiz, çıplak bir
        // "anahtar": "değer" parçası sıkıştırıyor — bu, JSON'ın tamamını SÖZ DİZİMİ olarak geçersiz
        // kılıyor (JToken.Parse baştan başarısız oluyor). Yine de metindeki dengeli { } bloklarını tarayıp
        // geçerli iki öneriyi kurtarabilmemiz gerekiyor.
        var fixture = new Fixture();
        fixture.LlmProvider
            .Setup(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                [
                 {"title":"Saha Etüdü","department":"Analiz","effortHours":10,"sourceDocument":null,"description":null,"sequenceNote":null,"insertAfterTaskTitle":null,"sequenceRank":1,"isAtProjectStart":true,"activities":[]},
                 "task": "gecersiz-parca",
                 {"title":"Zemin Testi","department":"Analiz","effortHours":8,"sourceDocument":null,"description":null,"sequenceNote":null,"insertAfterTaskTitle":null,"sequenceRank":2,"isAtProjectStart":true,"activities":[]}
                ]
                """);

        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: null);

        var result = await service.GenerateAsync(request);

        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.Title).Should().BeEquivalentTo(["Saha Etüdü", "Zemin Testi"]);
    }

    [Fact]
    public async Task GenerateAsync_ActivitiesArrayIsMalformedInsideAnOtherwiseValidObject_RestOfThatSuggestionSurvives()
    {
        // Canlıda gözlenen dördüncü bir arıza modu: bu sefer bozukluk en dış seviyede değil, ilk iş
        // paketinin "activities" dizisinin İÇİNDE — dış nesnenin kendisi (başlık/departman/süre) tamamen
        // sağlam. "activities" tamamen atılsa bile (nullable/isteğe bağlı olduğu için) geri kalan alanların
        // kurtarılabilmesi gerekiyor.
        var fixture = new Fixture();
        fixture.LlmProvider
            .Setup(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                [
                 {"title":"Saha Etüdü","department":"Analiz","effortHours":10,"sourceDocument":null,"description":null,"sequenceNote":null,"insertAfterTaskTitle":null,"sequenceRank":1,"isAtProjectStart":true,"activities":["gecersiz-parca": "deger"]},
                 {"title":"Zemin Testi","department":"Analiz","effortHours":8,"sourceDocument":null,"description":null,"sequenceNote":null,"insertAfterTaskTitle":null,"sequenceRank":2,"isAtProjectStart":true,"activities":[]}
                ]
                """);

        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: null);

        var result = await service.GenerateAsync(request);

        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.Title).Should().BeEquivalentTo(["Saha Etüdü", "Zemin Testi"]);
    }

    [Fact]
    public async Task GenerateAsync_ActivitiesFieldIsRepeatedMultipleTimesInsideOneObject_AllSuggestionsAreRecovered()
    {
        // Canlıda gözlenen beşinci bir arıza modu (2026-08-12): model "activities" alanını TEK bir dizi
        // içinde birden çok öğeyle yazacağına, aynı nesnede alanı BİRDEN FAZLA KEZ tekrarlıyor — her
        // tekrar da eskisi gibi süslü parantezsiz. Sadece İLK tekrarı boşaltmak yeterli değildi (geri
        // kalan tekrarlar hâlâ söz dizimini bozuyordu); gerçek olayda bu yüzden 5 öneriden 4'ü sessizce
        // kaybolmuştu. Tüm tekrarların boşaltılması gerekiyor.
        var fixture = new Fixture();
        fixture.LlmProvider
            .Setup(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                [
                 {"title":"Teknik Mimari","department":"Teknik Müdürlük","effortHours":80,"sourceDocument":null,"description":null,"sequenceNote":null,"insertAfterTaskTitle":null,"sequenceRank":1,"isAtProjectStart":true,"activities":["title":"A","effortHours":20],"activities":["title":"B","effortHours":15],"activities":["title":"C","effortHours":15]},
                 {"title":"Lojistik Otomasyon","department":"Teknik Müdürlük","effortHours":120,"sourceDocument":null,"description":null,"sequenceNote":null,"insertAfterTaskTitle":null,"sequenceRank":2,"isAtProjectStart":false,"activities":["title":"D","effortHours":25],"activities":["title":"E","effortHours":30]}
                ]
                """);

        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: null);

        var result = await service.GenerateAsync(request);

        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.Title).Should().BeEquivalentTo(["Teknik Mimari", "Lojistik Otomasyon"]);
        fixture.LlmProvider.Verify(
            p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_MostArrayElementsAreUnusableButNotZero_RetriesInsteadOfAcceptingASeverelyIncompleteBatch()
    {
        // "en az bir öneri kurtarıldı" (Count>0) tek başına yeterli bir başarı ölçütü değil: LLM'in
        // yanıtındaki 4 öğeden sadece 1'i kullanılabilirse (3 tanesi şemaya hiç uymayan başıboş metinse),
        // bu da tamamen boş bir yanıt kadar şüphelidir ve modele tekrar sorulmalı — aksi halde kullanıcı
        // beklediği iş paketlerinin büyük çoğunluğunu hiç görmeden "üretim başarılı" sonucunu alır
        // (2026-08-12 canlı olayının kök nedeni tam olarak buydu).
        var fixture = new Fixture();
        fixture.LlmProvider
            .SetupSequence(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""["baska-bir-metin","daha-baska-bir-metin","ucuncu-metin",{"title":"Saha Etüdü","department":"Analiz","effortHours":10,"sourceDocument":null,"description":null,"sequenceNote":null,"insertAfterTaskTitle":null,"sequenceRank":1,"isAtProjectStart":true,"activities":[]}]""")
            .ReturnsAsync("""
                [{"title":"Teknik Mimari","department":"Analiz","effortHours":10,"sourceDocument":null,"description":null,"sequenceNote":null,"insertAfterTaskTitle":null,"sequenceRank":1,"isAtProjectStart":true,"activities":[]},
                 {"title":"Lojistik Otomasyon","department":"Analiz","effortHours":10,"sourceDocument":null,"description":null,"sequenceNote":null,"insertAfterTaskTitle":null,"sequenceRank":2,"isAtProjectStart":false,"activities":[]}]
                """);

        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: null);

        var result = await service.GenerateAsync(request);

        result.Items.Select(i => i.Title).Should().BeEquivalentTo(
            ["Saha Etüdü", "Teknik Mimari", "Lojistik Otomasyon"],
            "the valid suggestion from the imperfect first response must be retained across retries");
        fixture.LlmProvider.Verify(
            p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GenerateAsync_FirstLlmResponseIsUnparseable_RetriesAndUsesTheSecondValidResponse()
    {
        var fixture = new Fixture();
        fixture.LlmProvider
            .SetupSequence(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("bu hiç JSON değil {{{")
            .ReturnsAsync("""[{"title":"Saha Etüdü","department":"Analiz","effortHours":10,"sourceDocument":null,"description":null,"sequenceNote":null,"insertAfterTaskTitle":null,"sequenceRank":1,"isAtProjectStart":true,"activities":[]}]""");

        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: null);

        var result = await service.GenerateAsync(request);

        result.Items.Should().ContainSingle();
        result.Items[0].Title.Should().Be("Saha Etüdü");
        fixture.LlmProvider.Verify(
            p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GenerateAsync_LlmKeepsFailingOnEveryAttempt_ThrowsAfterMaxAttemptsWithoutRetryingForever()
    {
        var fixture = new Fixture();
        fixture.LlmProvider
            .Setup(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("bu hiç JSON değil {{{");

        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: null);

        var act = () => service.GenerateAsync(request);

        await act.Should().ThrowAsync<DomainException>();
        fixture.LlmProvider.Verify(
            p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task GenerateAsync_RagProviderThrowsOnFirstAttempt_RetriesAndUsesSecondAttempt()
    {
        // ARGE-öncesi denetimde bulunan altıncı arıza modu (2026-08-12): RagLlmProvider'ın upload/
        // indeksleme/ask aşamalarından biri başarısız olup InvalidOperationException fırlattığında
        // (ör. RunPod pod'u yeniden başladı, indeksleme zaman aşımına uğradı) eskiden bu çağrı retry
        // döngüsünün DIŞINDAydı — tek seferlik bir RAG/altyapı aksaklığı hiç tekrar denenmeden tüm
        // üretimi anında düşürüyordu. Artık JSON bozulması gibi bu da tekrar denenmeli.
        var fixture = new Fixture();
        fixture.LlmProvider
            .SetupSequence(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("RAG iş paketi talimatlarını indeksleyemedi (bkz. önceki uyarı logu)."))
            .ReturnsAsync("""[{"title":"Saha Etüdü","department":"Analiz","effortHours":10,"sourceDocument":null,"description":null,"sequenceNote":null,"insertAfterTaskTitle":null,"sequenceRank":1,"isAtProjectStart":true,"activities":[]}]""");

        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: null);

        var result = await service.GenerateAsync(request);

        result.Items.Should().ContainSingle();
        result.Items[0].Title.Should().Be("Saha Etüdü");
        fixture.LlmProvider.Verify(
            p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GenerateAsync_RagProviderKeepsThrowingOnEveryAttempt_ThrowsDomainExceptionWithActionableMessageAfterMaxAttempts()
    {
        // Son denemede de RAG'e ulaşılamıyorsa, ham InvalidOperationException (ExceptionHandlingMiddleware'de
        // generic "Beklenmeyen bir hata oluştu" / HTTP 500'e düşerdi) yerine DomainException (400 + anlamlı
        // Türkçe mesaj) fırlatılmalı — sunum sırasında birinin bunu ayırt edebilmesi için.
        var fixture = new Fixture();
        fixture.LlmProvider
            .Setup(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("RAG iş paketi üretemedi: yanıt alınamadı"));

        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: null);

        var act = () => service.GenerateAsync(request);

        (await act.Should().ThrowAsync<DomainException>()).Which.Message.Should().Contain("RAG");
        fixture.LlmProvider.Verify(
            p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task GenerateAsync_RagProviderThrowsNetworkExceptionOnFirstAttempt_RetriesAndUsesSecondAttempt()
    {
        // ARGE-öncesi denetimde bulunan bir gedik: eski kod sadece InvalidOperationException'ı yakalıyordu.
        // RAG'e tamamen ulaşılamazsa (ör. yanlış/bayat RAG_BASE_URL, DNS/bağlantı hatası) RagClient'ın
        // HttpClient'ı HttpRequestException/TaskCanceledException fırlatabilir — bunlar da retry'a dahil
        // olmalı, tıpkı RagLlmProvider'ın kendi InvalidOperationException'ı gibi.
        var fixture = new Fixture();
        fixture.LlmProvider
            .SetupSequence(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("RAG'e bağlanılamadı"))
            .ReturnsAsync("""[{"title":"Saha Etüdü","department":"Analiz","effortHours":10,"sourceDocument":null,"description":null,"sequenceNote":null,"insertAfterTaskTitle":null,"sequenceRank":1,"isAtProjectStart":true,"activities":[]}]""");

        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: null);

        var result = await service.GenerateAsync(request);

        result.Items.Should().ContainSingle();
        fixture.LlmProvider.Verify(
            p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GenerateAsync_CallerCancelsRequest_DoesNotRetryAndPropagatesCancellation()
    {
        // Gerçek bir kullanıcı iptali (ör. istemci bağlantıyı kesti) ile RAG'in geçici arızasını birbirine
        // karıştırmamak lazım — ct zaten iptal edilmişse tekrar denemek anlamsızdır/israftır.
        var fixture = new Fixture();
        using var cts = new CancellationTokenSource();
        fixture.LlmProvider
            .Setup(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => cts.Cancel())
            .ThrowsAsync(new TaskCanceledException("İstemci bağlantıyı kesti"));

        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: null);

        var act = () => service.GenerateAsync(request, cts.Token);

        await act.Should().ThrowAsync<TaskCanceledException>();
        fixture.LlmProvider.Verify(
            p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_LlmOmitsEffortHoursAndIsAtProjectStart_SuggestionSurvivesWithSafeDefaults()
    {
        // ARGE-öncesi denetimde bulunan bir gedik: EffortHours (int) ve IsAtProjectStart (bool) non-nullable
        // olduğu için model bu alanlardan birine null/geçersiz bir değer yazdığında (ör. belirsiz kaldığında)
        // ToObject<T>() TÜM öneriyi (başlık/departman/açıklama gibi sağlam alanlarıyla birlikte) atıyordu.
        var fixture = new Fixture();
        fixture.LlmProvider
            .Setup(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""[{"title":"Belirsiz Süreli Görev","department":"Analiz","effortHours":null,"sourceDocument":null,"description":null,"sequenceNote":null,"insertAfterTaskTitle":null,"sequenceRank":1,"isAtProjectStart":null,"activities":[]}]""");

        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: null);

        var result = await service.GenerateAsync(request);

        result.Items.Should().ContainSingle();
        result.Items[0].Title.Should().Be("Belirsiz Süreli Görev");
        result.Items[0].EffortHours.Should().Be(0);
        result.Items[0].IsAtProjectStart.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateAsync_FinalAttemptStillLowYield_ResultIsFlaggedPossiblyIncomplete()
    {
        // Son denemede de öneriler yarısından azının kurtarılabildiği (ör. LLM'in üç denemede de aynı
        // bozuk kalıbı tekrarladığı) sessizce "başarılı" gibi dönmemeli — DTO'daki PossiblyIncomplete
        // alanı bunu frontend'e taşır.
        var fixture = new Fixture();
        fixture.LlmProvider
            .Setup(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""["sadece-bir-metin","baska-bir-metin","ucuncu-metin",{"title":"Saha Etüdü","department":"Analiz","effortHours":10,"sourceDocument":null,"description":null,"sequenceNote":null,"insertAfterTaskTitle":null,"sequenceRank":1,"isAtProjectStart":true,"activities":[]}]""");

        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: null);

        var result = await service.GenerateAsync(request);

        result.Items.Should().ContainSingle();
        result.PossiblyIncomplete.Should().BeTrue();
        fixture.LlmProvider.Verify(
            p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task GenerateAsync_HealthyResponse_ResultIsNotFlaggedPossiblyIncomplete()
    {
        var fixture = new Fixture();
        fixture.LlmProvider
            .Setup(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""[{"title":"Saha Etüdü","department":"Analiz","effortHours":10,"sourceDocument":null,"description":null,"sequenceNote":null,"insertAfterTaskTitle":null,"sequenceRank":1,"isAtProjectStart":true,"activities":[]}]""");

        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: null);

        var result = await service.GenerateAsync(request);

        result.PossiblyIncomplete.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateAsync_FirstResponseOmitsTitle_RetriesInsteadOfThrowingArgumentNullException()
    {
        var fixture = new Fixture();
        fixture.LlmProvider
            .SetupSequence(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""[{"department":"Analiz","effortHours":10,"activities":[]}]""")
            .ReturnsAsync("""[{"title":"Saha Etüdü","department":"Analiz","effortHours":10,"activities":[]}]""");

        var service = fixture.CreateService();
        var result = await service.GenerateAsync(new GenerateSuggestionsRequest(ProjectId, null, null));

        result.Items.Should().ContainSingle().Which.Title.Should().Be("Saha Etüdü");
        fixture.LlmProvider.Verify(
            p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GenerateAsync_FirstResponseOmitsDepartment_RetriesInsteadOfReachingDatabaseWithNull()
    {
        var fixture = new Fixture();
        fixture.LlmProvider
            .SetupSequence(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""[{"title":"Saha Etüdü","effortHours":10,"activities":[]}]""")
            .ReturnsAsync("""[{"title":"Saha Etüdü","department":"Analiz","effortHours":10,"activities":[]}]""");

        var service = fixture.CreateService();
        var result = await service.GenerateAsync(new GenerateSuggestionsRequest(ProjectId, null, null));

        result.Items.Should().ContainSingle().Which.Department.Should().Be("Analiz");
        fixture.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_MixedValidAndMissingRequiredFields_RetainsValidAndAsksOnlyForCorrection()
    {
        var fixture = new Fixture();
        var prompts = new List<string>();
        var responses = new Queue<string>(
        [
            """
            [{"title":"Geçerli Paket","department":"Analiz","effortHours":10,"activities":[]},
             {"department":"Analiz","effortHours":8,"activities":[]}]
            """,
            """[{"title":"Düzeltilen Paket","department":"Analiz","effortHours":8,"activities":[]}]"""
        ]);
        fixture.LlmProvider
            .Setup(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((prompt, _) => prompts.Add(prompt))
            .ReturnsAsync(() => responses.Dequeue());

        var service = fixture.CreateService();
        var result = await service.GenerateAsync(new GenerateSuggestionsRequest(ProjectId, null, null));

        result.Items.Select(i => i.Title).Should().BeEquivalentTo(["Geçerli Paket", "Düzeltilen Paket"]);
        prompts.Should().HaveCount(2);
        prompts[1].Should().Contain("Eksik veya hatalı alanlar: title");
        prompts[1].Should().Contain("Geçerli Paket").And.Contain("bunları tekrar üretme");
    }

    [Fact]
    public async Task GenerateAsync_ActivityWithoutTitle_RetriesAndUsesCorrectedSuggestion()
    {
        var fixture = new Fixture();
        fixture.LlmProvider
            .SetupSequence(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""[{"title":"Saha Etüdü","department":"Analiz","effortHours":10,"activities":[{"effortHours":4}]}]""")
            .ReturnsAsync("""[{"title":"Saha Etüdü","department":"Analiz","effortHours":10,"activities":[{"title":"Ölçüm yap","effortHours":4}]}]""");

        var service = fixture.CreateService();
        var result = await service.GenerateAsync(new GenerateSuggestionsRequest(ProjectId, null, null));

        result.Items.Should().ContainSingle();
        result.Items[0].Activities.Should().ContainSingle().Which.Title.Should().Be("Ölçüm yap");
    }

    [Fact]
    public async Task GenerateAsync_AllAttemptsMissRequiredFields_ThrowsControlledDomainErrorWithoutSaving()
    {
        var fixture = new Fixture();
        fixture.LlmProvider
            .Setup(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""[{"description":"Başlık ve departman yok","activities":[]}]""");

        var service = fixture.CreateService();
        var act = () => service.GenerateAsync(new GenerateSuggestionsRequest(ProjectId, null, null));

        (await act.Should().ThrowAsync<DomainException>()).Which.Message.Should().Contain("zorunlu");
        fixture.LlmProvider.Verify(
            p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        fixture.Requests.Verify(
            r => r.AddAsync(It.IsAny<AiSuggestionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GenerateAsync_ArrayContainsOnlyGarbageElements_RetriesInsteadOfReportingGenuinelyEmpty()
    {
        // "[]" (gerçekten boş) ile "her öğesi çöp olan bir dizi" birbirinden ayrılmalı — ikincisi de
        // suggestions.Count==0 üretir ama bu meşru bir "önerecek bir şey yok" durumu değil, tekrar
        // denenmesi gereken şüpheli bir durumdur.
        var fixture = new Fixture();
        fixture.LlmProvider
            .SetupSequence(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""["sadece-bir-metin", "baska-bir-metin"]""")
            .ReturnsAsync("""[{"title":"Saha Etüdü","department":"Analiz","effortHours":10,"sourceDocument":null,"description":null,"sequenceNote":null,"insertAfterTaskTitle":null,"sequenceRank":1,"isAtProjectStart":true,"activities":[]}]""");

        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: null);

        var result = await service.GenerateAsync(request);

        result.Items.Should().ContainSingle();
        fixture.LlmProvider.Verify(
            p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GenerateAsync_LlmGenuinelyReturnsNoSuggestions_DoesNotRetry()
    {
        var fixture = new Fixture();
        fixture.LlmProvider
            .Setup(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("[]");

        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: null);

        var result = await service.GenerateAsync(request);

        result.Items.Should().BeEmpty();
        fixture.LlmProvider.Verify(
            p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_TwoNewSuggestionsInSameResponseAreNearDuplicates_OnlyFirstIsKept()
    {
        var fixture = new Fixture();
        fixture.LlmProvider
            .Setup(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                [{"title":"Ruhsat İzin Süreçleri","department":"Analiz","effortHours":10,"sourceDocument":null,"description":null,"sequenceNote":null,"insertAfterTaskTitle":null,"sequenceRank":1,"isAtProjectStart":true,"activities":[]},
                 {"title":"Ruhsat/İzin Süreçleri","department":"Analiz","effortHours":10,"sourceDocument":null,"description":null,"sequenceNote":null,"insertAfterTaskTitle":null,"sequenceRank":2,"isAtProjectStart":true,"activities":[]}]
                """);

        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: null);

        var result = await service.GenerateAsync(request);

        result.Items.Should().ContainSingle(
            "the second suggestion is a punctuation-only rewrite of the first, from the very same response");
    }

    [Fact]
    public async Task GenerateAsync_InsertAfterTaskTitleIsNotARealExistingTask_IsClearedToNull()
    {
        var fixture = new Fixture();
        var existingTasks = new List<ExistingTaskInfoDto> { new("Zemin Etüdü", null, "Done", null, null) };
        fixture.TaskInfoClient.Setup(c => c.ListExistingTasksAsync(ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync(existingTasks);
        // Simulates the LLM confusing a document-excerpt citation label (e.g. "[Doküman: X (section: Y)]")
        // with a real task title — it looks like a heading but was never in the existing-tasks list.
        fixture.LlmProvider
            .Setup(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""[{"title":"Yeni Öneri","department":"Analiz","effortHours":10,"sourceDocument":null,"description":null,"sequenceNote":"Bu bölümden sonra yapılır","insertAfterTaskTitle":"12. Ayrıntılı Faaliyet Kayıtları (Devam 27-52)","sequenceRank":1,"isAtProjectStart":false,"activities":[]}]""");

        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: null);

        var result = await service.GenerateAsync(request);

        result.Items.Should().ContainSingle().Which.InsertAfterTaskTitle.Should().BeNull(
            "the referenced title never appeared in the real existing-task list, so it must not be trusted");
    }

    [Fact]
    public async Task GenerateAsync_SequenceNoteEchoesFieldNameOrInsertAfterValue_IsClearedToNull()
    {
        var fixture = new Fixture();
        fixture.LlmProvider
            .Setup(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                [{"title":"Öneri A","department":"Analiz","effortHours":10,"sourceDocument":null,"description":null,"sequenceNote":"insertAfterTaskTitle","insertAfterTaskTitle":null,"sequenceRank":1,"isAtProjectStart":true,"activities":[]},
                 {"title":"Öneri B","department":"Analiz","effortHours":10,"sourceDocument":null,"description":null,"sequenceNote":"12. Ayrıntılı Faaliyet Kayıtları","insertAfterTaskTitle":"12. Ayrıntılı Faaliyet Kayıtları","sequenceRank":2,"isAtProjectStart":false,"activities":[]}]
                """);

        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: null);

        var result = await service.GenerateAsync(request);

        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(i => i.SequenceNote == null,
            "one note is the literal field name, the other duplicates the (invalid) insertAfterTaskTitle value — both are model glitches, not real notes");
    }

    [Fact]
    public async Task GenerateAsync_ExistingTaskRetrievalFails_TitlesToSkipStillFiltersExactMatchFromFullList()
    {
        var fixture = new Fixture();
        var existingTasks = new List<ExistingTaskInfoDto> { new("Zemin Etüdü", null, "Done", null, null) };
        fixture.TaskInfoClient.Setup(c => c.ListExistingTasksAsync(ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync(existingTasks);
        // Simulates RAG failure/timeout for the existing-task retrieval — prompt section is empty,
        // but the deterministic post-filter below must NOT be affected by this.
        fixture.WorkPackageContextRetrievalService
            .Setup(s => s.RetrieveExistingTaskContextAsync(
                It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ExistingTaskInfoDto>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<string>)[]);
        fixture.LlmProvider
            .Setup(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""[{"title":"Zemin Etüdü","department":"Analiz","effortHours":10,"sourceDocument":null,"description":null,"sequenceNote":null,"insertAfterTaskTitle":null,"sequenceRank":1,"isAtProjectStart":true,"activities":[]}]""");

        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: null);

        var result = await service.GenerateAsync(request);

        result.Items.Should().BeEmpty("the LLM re-proposed an existing task title, which titlesToSkip must reject regardless of what RAG retrieved for the prompt");
    }

    [Fact]
    public async Task GenerateAsync_PendingSuggestionRetrievalFails_TitlesToSkipStillFiltersExactMatchFromFullList()
    {
        var fixture = new Fixture();
        var domainRequest = AiSuggestionRequest.Create(ProjectId, Project.Type, null, "prompt", "Mock", null, false);
        domainRequest.AddItem("Saha Analizi", "Analiz", 10, null, null, null, null, null, false);
        fixture.Requests.Setup(r => r.ListByProjectAsync(ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync([domainRequest]);
        fixture.WorkPackageContextRetrievalService
            .Setup(s => s.RetrievePendingSuggestionContextAsync(
                It.IsAny<Guid>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<string>)[]);
        fixture.LlmProvider
            .Setup(p => p.GenerateWorkPackagesJsonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("""[{"title":"Saha Analizi","department":"Analiz","effortHours":10,"sourceDocument":null,"description":null,"sequenceNote":null,"insertAfterTaskTitle":null,"sequenceRank":1,"isAtProjectStart":true,"activities":[]}]""");

        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: null);

        var result = await service.GenerateAsync(request);

        result.Items.Should().BeEmpty("the LLM re-proposed a pending suggestion's title, which titlesToSkip must reject regardless of what RAG retrieved for the prompt");
    }

    [Fact]
    public async Task GenerateAsync_ContextRetrievalReturnsContexts_AppendsThemToPrompt()
    {
        var fixture = new Fixture();
        var existingTasks = new List<ExistingTaskInfoDto> { new("Zemin Etüdü", null, "Done", null, null) };
        fixture.TaskInfoClient.Setup(c => c.ListExistingTasksAsync(ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync(existingTasks);
        fixture.WorkPackageContextRetrievalService
            .Setup(s => s.RetrieveExistingTaskContextAsync(
                It.IsAny<Guid>(), It.IsAny<IReadOnlyList<ExistingTaskInfoDto>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<string>)["1. \"Zemin Etüdü\" — durum: Done, tarih planlanmamış"]);
        string? capturedPrompt = null;
        fixture.AuditLogger
            .Setup(a => a.LogAsync(ProjectId, "Mock", It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, string, string, IReadOnlyList<string>, CancellationToken>((_, _, prompt, _, _) => capturedPrompt = prompt)
            .Returns(Task.CompletedTask);

        var service = fixture.CreateService();
        var request = new GenerateSuggestionsRequest(ProjectId, null, SelectedDocumentIds: null);

        await service.GenerateAsync(request);

        capturedPrompt.Should().Contain("Zemin Etüdü");
    }
}
