using FluentAssertions;
using Moq;
using Ozdilek.PM.ProjectService.Application.Dtos;
using Ozdilek.PM.ProjectService.Application.Interfaces;
using Ozdilek.PM.ProjectService.Application.Services;
using Ozdilek.PM.ProjectService.Domain;
using Ozdilek.PM.SharedKernel.Events;
using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;
using Xunit;

namespace Ozdilek.PM.ProjectService.Tests;

public class ProjectAppServiceTests
{
    private static readonly Guid ManagerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
    private static readonly Guid DepartmentId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");

    private static Project CreateProject(ProjectType type) => Project.Create(
        "Mağaza Yenileme", "açıklama", "Ahmet Görür", null, "Arge Proje Müdürlüğü",
        type, 500_000m, "TRY", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 1), null, []);

    private static ProjectAppService CreateSut(
        Project project, out Mock<IFeasibilityInfoClient> feasibilityMock, bool feasibilityApproved = false)
    {
        var repository = new Mock<IProjectRepository>();
        repository.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>())).ReturnsAsync(project);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        feasibilityMock = new Mock<IFeasibilityInfoClient>();
        feasibilityMock
            .Setup(f => f.IsFullyApprovedAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feasibilityApproved);

        return new ProjectAppService(
            repository.Object,
            new Mock<IProjectTemplateRepository>().Object,
            unitOfWork.Object,
            feasibilityMock.Object,
            CreateDirectoryMock().Object,
            new Mock<IEventPublisher>().Object);
    }

    [Fact]
    public async Task ActivateAsync_FeasibilityBased_WithoutApproval_Throws()
    {
        var project = CreateProject(ProjectType.FeasibilityBased);
        var sut = CreateSut(project, out _, feasibilityApproved: false);

        var act = () => sut.ActivateAsync(project.Id);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*Fizibilitesi onaylanmamış*");
        project.Status.Should().Be(ProjectStatus.Draft);
    }

    [Fact]
    public async Task ActivateAsync_FeasibilityBased_WithApproval_Succeeds()
    {
        var project = CreateProject(ProjectType.FeasibilityBased);
        var sut = CreateSut(project, out _, feasibilityApproved: true);

        await sut.ActivateAsync(project.Id);

        project.Status.Should().Be(ProjectStatus.Active);
    }

    [Fact]
    public async Task ActivateAsync_SimpleProject_DoesNotConsultFeasibilityService()
    {
        var project = CreateProject(ProjectType.Simple);
        var sut = CreateSut(project, out var feasibilityMock, feasibilityApproved: false);

        await sut.ActivateAsync(project.Id);

        project.Status.Should().Be(ProjectStatus.Active);
        feasibilityMock.Verify(f => f.IsFullyApprovedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelAsync_ActiveProject_MarksCancelled()
    {
        var project = CreateProject(ProjectType.Simple);
        project.Activate();
        var sut = CreateSut(project, out _);

        await sut.CancelAsync(project.Id);

        project.Status.Should().Be(ProjectStatus.Cancelled);
    }

    [Fact]
    public async Task CreateAsync_TemplateTypeDoesNotMatch_Throws()
    {
        var projectRepository = CreateEmptyProjectRepositoryMock();
        var templateRepository = new Mock<IProjectTemplateRepository>();
        var template = ProjectTemplate.Create("Çok Birimli Standart", ProjectType.MultiUnit);
        templateRepository
            .Setup(repository => repository.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var sut = new ProjectAppService(
            projectRepository.Object,
            templateRepository.Object,
            new Mock<IUnitOfWork>().Object,
            new Mock<IFeasibilityInfoClient>().Object,
            CreateDirectoryMock().Object,
            new Mock<IEventPublisher>().Object);

        var request = CreateRequest(ProjectType.Simple, template.Id, []);

        var act = () => sut.CreateAsync(request);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*proje türüyle*");
    }

    [Fact]
    public async Task CreateAsync_RequiredTemplateFieldMissing_Throws()
    {
        var projectRepository = CreateEmptyProjectRepositoryMock();
        var templateRepository = new Mock<IProjectTemplateRepository>();
        var template = ProjectTemplate.Create("Basit Proje Kontrolü", ProjectType.Simple);
        template.AddField("İş Gerekçesi", "Gerekçeyi yazın", "textarea", null, isRequired: true);
        templateRepository
            .Setup(repository => repository.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var sut = new ProjectAppService(
            projectRepository.Object,
            templateRepository.Object,
            new Mock<IUnitOfWork>().Object,
            new Mock<IFeasibilityInfoClient>().Object,
            CreateDirectoryMock().Object,
            new Mock<IEventPublisher>().Object);

        var request = CreateRequest(ProjectType.Simple, template.Id, []);

        var act = () => sut.CreateAsync(request);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*İş Gerekçesi*zorunludur*");
    }

    [Fact]
    public async Task CreateAsync_WithTemplateValue_PersistsSnapshot()
    {
        var projectRepository = CreateEmptyProjectRepositoryMock();
        var templateRepository = new Mock<IProjectTemplateRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var template = ProjectTemplate.Create("Basit Proje Kontrolü", ProjectType.Simple);
        template.AddField(
            "Proje Adı",
            "Proje adını girin",
            "text",
            null,
            isRequired: true,
            kind: TemplateFieldKind.System,
            systemKey: "projectName");
        var field = template.AddField("İş Gerekçesi", "Gerekçeyi yazın", "textarea", null, isRequired: true);
        templateRepository
            .Setup(repository => repository.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var sut = new ProjectAppService(
            projectRepository.Object,
            templateRepository.Object,
            unitOfWork.Object,
            new Mock<IFeasibilityInfoClient>().Object,
            CreateDirectoryMock().Object,
            new Mock<IEventPublisher>().Object);

        var result = await sut.CreateAsync(CreateRequest(
            ProjectType.Simple,
            template.Id,
            [new TemplateFieldValueRequest(field.Id, "Operasyonel verimlilik")]));

        result.TemplateName.Should().Be(template.Name);
        result.TemplateValues.Should().ContainSingle(value =>
            value.Label == "İş Gerekçesi" && value.Value == "Operasyonel verimlilik");
        result.TemplateValues.Should().NotContain(value => value.Label == "Proje Adı");
        result.ManagerEmployeeId.Should().Be(ManagerId);
        result.UnitDepartmentId.Should().Be(DepartmentId);
        result.ManagerName.Should().Be("Ahmet Görür");
        result.Unit.Should().Be("Arge");
        projectRepository.Verify(repository => repository.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InactiveManager_Throws()
    {
        var directory = CreateDirectoryMock();
        directory
            .Setup(client => client.GetEmployeeAsync(ManagerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DirectoryEmployee(
                ManagerId,
                "Pasif Yönetici",
                DepartmentId,
                ["ProjectManager"],
                IsActive: false));
        var sut = new ProjectAppService(
            CreateEmptyProjectRepositoryMock().Object,
            new Mock<IProjectTemplateRepository>().Object,
            new Mock<IUnitOfWork>().Object,
            new Mock<IFeasibilityInfoClient>().Object,
            directory.Object,
            new Mock<IEventPublisher>().Object);

        var act = () => sut.CreateAsync(CreateRequest(ProjectType.Simple, null, []));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*pasif bir çalışan*");
    }

    [Fact]
    public async Task CreateAsync_ArchivedUnit_Throws()
    {
        var directory = CreateDirectoryMock();
        directory
            .Setup(client => client.GetDepartmentAsync(DepartmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DirectoryDepartment(
                DepartmentId,
                "Arşiv Birimi",
                ManagerId,
                IsActive: false));
        var sut = new ProjectAppService(
            CreateEmptyProjectRepositoryMock().Object,
            new Mock<IProjectTemplateRepository>().Object,
            new Mock<IUnitOfWork>().Object,
            new Mock<IFeasibilityInfoClient>().Object,
            directory.Object,
            new Mock<IEventPublisher>().Object);

        var act = () => sut.CreateAsync(CreateRequest(ProjectType.Simple, null, []));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*Arşivlenmiş departman*");
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_Throws()
    {
        var existing = Project.Create(
            "Yeni Proje", "açıklama", "Ahmet Görür", null, "Arge",
            ProjectType.Simple, 10_000m, "TRY", new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 1), null, []);
        var projectRepository = new Mock<IProjectRepository>();
        projectRepository
            .Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Project, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existing]);
        var sut = new ProjectAppService(
            projectRepository.Object,
            new Mock<IProjectTemplateRepository>().Object,
            new Mock<IUnitOfWork>().Object,
            new Mock<IFeasibilityInfoClient>().Object,
            CreateDirectoryMock().Object,
            new Mock<IEventPublisher>().Object);

        var act = () => sut.CreateAsync(CreateRequest(ProjectType.Simple, null, []));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*isimde bir proje zaten mevcut*");
    }

    private static CreateProjectRequest CreateRequest(
        ProjectType type,
        Guid? templateId,
        List<TemplateFieldValueRequest> templateValues) => new(
            "Yeni Proje",
            "Açıklama",
            "Ahmet Görür",
            null,
            "Arge",
            type,
            100_000,
            "TRY",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 3, 1),
            templateId,
            ["description"],
            templateValues,
            [],
            ManagerId,
            null,
            DepartmentId);

    private static Mock<IProjectRepository> CreateEmptyProjectRepositoryMock()
    {
        var repository = new Mock<IProjectRepository>();
        repository
            .Setup(r => r.ListAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Project, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        return repository;
    }

    private static Mock<IUserDirectoryClient> CreateDirectoryMock()
    {
        var directory = new Mock<IUserDirectoryClient>();
        directory
            .Setup(client => client.GetEmployeeAsync(ManagerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DirectoryEmployee(
                ManagerId,
                "Ahmet Görür",
                DepartmentId,
                ["ProjectManager"]));
        directory
            .Setup(client => client.GetDepartmentAsync(DepartmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DirectoryDepartment(DepartmentId, "Arge", ManagerId));
        return directory;
    }
}
