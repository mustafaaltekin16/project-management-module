using FluentAssertions;
using Moq;
using Ozdilek.PM.ProjectService.Application.Dtos;
using Ozdilek.PM.ProjectService.Application.Interfaces;
using Ozdilek.PM.ProjectService.Application.Services;
using Ozdilek.PM.ProjectService.Domain;
using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;
using Xunit;

namespace Ozdilek.PM.ProjectService.Tests;

public class ProjectTemplateAppServiceTests
{
    private static Mock<IProjectTemplateRepository> CreateEmptyTemplateRepositoryMock()
    {
        var repository = new Mock<IProjectTemplateRepository>();
        repository
            .Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProjectTemplate, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        return repository;
    }

    [Fact]
    public async Task CreateAsync_MissingMandatorySystemField_Throws()
    {
        var sut = new ProjectTemplateAppService(
            CreateEmptyTemplateRepositoryMock().Object,
            new Mock<IUnitOfWork>().Object);
        var request = new CreateTemplateRequest(
            "Eksik Şablon",
            ProjectType.Simple,
            [Field("Proje Adı", "text", TemplateFieldKind.System, "projectName", required: true)]);

        var act = () => sut.CreateAsync(request);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*unit*sistem alanı*");
    }

    [Fact]
    public async Task CreateAsync_CompleteSchema_PersistsKindsAndOptions()
    {
        var repository = CreateEmptyTemplateRepositoryMock();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var sut = new ProjectTemplateAppService(repository.Object, unitOfWork.Object);
        var fields = RequiredSimpleSystemFields();
        fields.Add(new CreateTemplateFieldRequest(
            "Risk Seviyesi", "Risk seçin", "select", "manual", true, true,
            TemplateFieldKind.Custom, null, ["Düşük", "Yüksek"]));

        var result = await sut.CreateAsync(new CreateTemplateRequest("Standart", ProjectType.Simple, fields));

        result.Fields.Should().Contain(field =>
            field.Kind == TemplateFieldKind.Custom &&
            field.Label == "Risk Seviyesi" &&
            field.Options.SequenceEqual(new[] { "Düşük", "Yüksek" }));
        repository.Verify(repo => repo.AddAsync(It.IsAny<ProjectTemplate>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_Throws()
    {
        var existing = ProjectTemplate.Create("Standart", ProjectType.Simple);
        var repository = new Mock<IProjectTemplateRepository>();
        repository
            .Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProjectTemplate, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existing]);
        var sut = new ProjectTemplateAppService(repository.Object, new Mock<IUnitOfWork>().Object);

        var act = () => sut.CreateAsync(new CreateTemplateRequest("Standart", ProjectType.Simple, RequiredSimpleSystemFields()));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*isimde bir şablon zaten mevcut*");
    }

    [Fact]
    public async Task UpdateAsync_RenamingToAnotherTemplatesName_Throws()
    {
        var other = ProjectTemplate.Create("Diğer Şablon", ProjectType.Simple);
        var target = ProjectTemplate.Create("Hedef Şablon", ProjectType.Simple);
        var repository = new Mock<IProjectTemplateRepository>();
        repository
            .Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProjectTemplate, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([other]);
        repository.Setup(r => r.GetByIdAsync(target.Id, It.IsAny<CancellationToken>())).ReturnsAsync(target);
        var sut = new ProjectTemplateAppService(repository.Object, new Mock<IUnitOfWork>().Object);

        var act = () => sut.UpdateAsync(
            target.Id, new UpdateTemplateRequest("Diğer Şablon", ProjectType.Simple, RequiredSimpleSystemFields()));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*isimde bir şablon zaten mevcut*");
    }

    [Fact]
    public async Task UpdateAsync_KeepingOwnName_Succeeds()
    {
        // Mock'ta gerçek "t.Id != id" filtresi çalışmaz (Expression çalıştırılmıyor, sadece
        // eşleşiyor) — bu yüzden burada gerçek kod yolunun kendi kaydını hariç tutunca
        // döndüreceği sonucu (boş liste) simüle ediyoruz.
        var target = ProjectTemplate.Create("Hedef Şablon", ProjectType.Simple);
        var repository = new Mock<IProjectTemplateRepository>();
        repository
            .Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProjectTemplate, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        repository.Setup(r => r.GetByIdAsync(target.Id, It.IsAny<CancellationToken>())).ReturnsAsync(target);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var sut = new ProjectTemplateAppService(repository.Object, unitOfWork.Object);

        var result = await sut.UpdateAsync(
            target.Id, new UpdateTemplateRequest("Hedef Şablon", ProjectType.Simple, RequiredSimpleSystemFields()));

        result.Name.Should().Be("Hedef Şablon");
    }

    private static List<CreateTemplateFieldRequest> RequiredSimpleSystemFields() =>
    [
        Field("Proje Adı", "text", TemplateFieldKind.System, "projectName", required: true),
        Field("Birim", "text", TemplateFieldKind.System, "unit", required: true),
        Field("Başlangıç Tarihi", "date", TemplateFieldKind.System, "startDate", required: true),
        Field("Bitiş Tarihi", "date", TemplateFieldKind.System, "endDate", required: true),
        Field("Proje Yöneticisi", "employee", TemplateFieldKind.System, "manager", required: true)
    ];

    private static CreateTemplateFieldRequest Field(
        string label,
        string contentType,
        TemplateFieldKind kind,
        string? systemKey,
        bool required) =>
        new(label, "", contentType, null, required, true, kind, systemKey, []);
}
