using FluentAssertions;
using Moq;
using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.SharedKernel.Persistence;
using Ozdilek.PM.UserDirectoryService.Application.Dtos;
using Ozdilek.PM.UserDirectoryService.Application.Interfaces;
using Ozdilek.PM.UserDirectoryService.Application.Services;
using Ozdilek.PM.UserDirectoryService.Domain;

namespace Ozdilek.PM.UserDirectoryService.Tests;

public class DirectoryConsistencyTests
{
    [Fact]
    public async Task AssignHeadAsync_MovesEmployeeIntoDepartment()
    {
        var target = Department.Create("BT", null);
        var employee = CreateEmployee();
        var departmentRepository = new Mock<IDepartmentRepository>();
        departmentRepository
            .Setup(repository => repository.GetByIdAsync(target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);
        departmentRepository
            .Setup(repository => repository.ListByHeadEmployeeIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var employeeRepository = new Mock<IEmployeeRepository>();
        employeeRepository
            .Setup(repository => repository.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        employeeRepository
            .Setup(repository => repository.CountByDepartmentAsync(target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var service = new DepartmentAppService(
            departmentRepository.Object,
            employeeRepository.Object,
            unitOfWork.Object);

        await service.AssignHeadAsync(target.Id, new AssignDepartmentHeadRequest(employee.Id));

        target.HeadEmployeeId.Should().Be(employee.Id);
        employee.DepartmentId.Should().Be(target.Id);
    }

    [Fact]
    public async Task AssignHeadAsync_WhenEmployeeHeadsAnotherDepartment_Throws()
    {
        var current = Department.Create("Mevcut", null);
        var target = Department.Create("Hedef", null);
        var employee = CreateEmployee();
        current.AssignHead(employee.Id);
        var departmentRepository = new Mock<IDepartmentRepository>();
        departmentRepository
            .Setup(repository => repository.GetByIdAsync(target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);
        departmentRepository
            .Setup(repository => repository.ListByHeadEmployeeIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([current]);
        var employeeRepository = new Mock<IEmployeeRepository>();
        employeeRepository
            .Setup(repository => repository.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        var service = new DepartmentAppService(
            departmentRepository.Object,
            employeeRepository.Object,
            new Mock<IUnitOfWork>().Object);

        var act = () => service.AssignHeadAsync(target.Id, new AssignDepartmentHeadRequest(employee.Id));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*mevcut sorumluluk*");
        target.HeadEmployeeId.Should().BeNull();
    }

    [Fact]
    public async Task AssignDepartmentAsync_WhenEmployeeIsDepartmentHead_Throws()
    {
        var current = Department.Create("Mevcut", null);
        var target = Department.Create("Hedef", null);
        var employee = CreateEmployee();
        current.AssignHead(employee.Id);
        employee.AssignDepartment(current.Id);
        var employeeRepository = new Mock<IEmployeeRepository>();
        employeeRepository
            .Setup(repository => repository.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        var departmentRepository = new Mock<IDepartmentRepository>();
        departmentRepository
            .Setup(repository => repository.GetByIdAsync(target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);
        departmentRepository
            .Setup(repository => repository.ListByHeadEmployeeIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([current]);
        var service = new EmployeeAppService(
            employeeRepository.Object,
            departmentRepository.Object,
            new Mock<IPasswordHasherService>().Object,
            new Mock<IUnitOfWork>().Object);

        var act = () => service.AssignDepartmentAsync(
            employee.Id,
            new AssignEmployeeDepartmentRequest(target.Id),
            allowElevatedAccounts: true);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*sorumluluğu kaldırılmalıdır*");
        employee.DepartmentId.Should().Be(current.Id);
    }

    [Fact]
    public async Task CreateEmployeeAsync_ProjectManagerCannotGrantElevatedRoles()
    {
        var employeeRepository = new Mock<IEmployeeRepository>();
        employeeRepository
            .Setup(repository => repository.ExistsWithEmailAsync(
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var service = new EmployeeAppService(
            employeeRepository.Object,
            new Mock<IDepartmentRepository>().Object,
            new Mock<IPasswordHasherService>().Object,
            new Mock<IUnitOfWork>().Object);

        var act = () => service.CreateAsync(
            new CreateEmployeeRequest(
                "Yeni Yönetici",
                "manager@example.com",
                "test123",
                null,
                "Yönetici",
                ["ProjectManager"]),
            allowElevatedRoles: false);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*yalnızca Admin*");
    }

    [Fact]
    public void DepartmentArchive_ClearsHeadAndMarksInactive()
    {
        var department = Department.Create("Operasyon", Guid.NewGuid());

        department.SetActive(false);

        department.IsActive.Should().BeFalse();
        department.HeadEmployeeId.Should().BeNull();
        department.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task SetEmployeeStatusAsync_WhenDepartmentIsArchived_CannotReactivate()
    {
        var department = Department.Create("Arşiv", null);
        department.SetActive(false);
        var employee = CreateEmployee();
        employee.AssignDepartment(department.Id);
        employee.SetActive(false);
        var employeeRepository = new Mock<IEmployeeRepository>();
        employeeRepository
            .Setup(repository => repository.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        var departmentRepository = new Mock<IDepartmentRepository>();
        departmentRepository
            .Setup(repository => repository.GetByIdAsync(department.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);
        var service = new EmployeeAppService(
            employeeRepository.Object,
            departmentRepository.Object,
            new Mock<IPasswordHasherService>().Object,
            new Mock<IUnitOfWork>().Object);

        var act = () => service.SetStatusAsync(employee.Id, new SetEmployeeStatusRequest(true));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*Arşivlenmiş departmana*");
        employee.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateEmployeeAsync_ProjectManagerCannotEditElevatedAccount()
    {
        var employee = Employee.Create(
            "Proje Yöneticisi",
            "manager@example.com",
            "password-hash",
            null,
            "Yönetici",
            ["ProjectManager"]);
        var employeeRepository = new Mock<IEmployeeRepository>();
        employeeRepository
            .Setup(repository => repository.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        var service = new EmployeeAppService(
            employeeRepository.Object,
            new Mock<IDepartmentRepository>().Object,
            new Mock<IPasswordHasherService>().Object,
            new Mock<IUnitOfWork>().Object);

        var act = () => service.UpdateAsync(
            employee.Id,
            new UpdateEmployeeRequest(
                "Değiştirilen",
                employee.Email,
                null,
                employee.Title,
                ["Member"]),
            allowElevatedRoles: false);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*yalnızca Admin*");
        employee.DisplayName.Should().Be("Proje Yöneticisi");
    }

    [Fact]
    public async Task CreateEmployeeAsync_AdminSystemAccountCannotBelongToDepartment()
    {
        var department = Department.Create("BT", null);
        var employeeRepository = new Mock<IEmployeeRepository>();
        employeeRepository
            .Setup(repository => repository.ExistsWithEmailAsync(
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var service = new EmployeeAppService(
            employeeRepository.Object,
            new Mock<IDepartmentRepository>().Object,
            new Mock<IPasswordHasherService>().Object,
            new Mock<IUnitOfWork>().Object);

        var act = () => service.CreateAsync(
            new CreateEmployeeRequest(
                "İkinci Admin",
                "admin2@example.com",
                "test123",
                department.Id,
                "Sistem Yöneticisi",
                ["Admin"]),
            allowElevatedRoles: true);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*bir departmana bağlanamaz*");
    }

    [Fact]
    public async Task DeleteEmployeeAsync_RequiresInactiveNonAdminWithoutHeadResponsibility()
    {
        var employee = CreateEmployee();
        employee.SetActive(false);
        var employeeRepository = new Mock<IEmployeeRepository>();
        employeeRepository
            .Setup(repository => repository.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        var departmentRepository = new Mock<IDepartmentRepository>();
        departmentRepository
            .Setup(repository => repository.ListByHeadEmployeeIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var service = new EmployeeAppService(
            employeeRepository.Object,
            departmentRepository.Object,
            new Mock<IPasswordHasherService>().Object,
            unitOfWork.Object);

        await service.DeleteAsync(employee.Id);

        employeeRepository.Verify(repository => repository.Remove(employee), Times.Once);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteDepartmentAsync_RequiresArchivedDepartmentWithoutAnyEmployees()
    {
        var department = Department.Create("Eski Birim", null);
        department.SetActive(false);
        var departmentRepository = new Mock<IDepartmentRepository>();
        departmentRepository
            .Setup(repository => repository.GetByIdAsync(department.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);
        var employeeRepository = new Mock<IEmployeeRepository>();
        employeeRepository
            .Setup(repository => repository.CountAllByDepartmentAsync(department.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(work => work.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var service = new DepartmentAppService(
            departmentRepository.Object,
            employeeRepository.Object,
            unitOfWork.Object);

        await service.DeleteAsync(department.Id);

        departmentRepository.Verify(repository => repository.Remove(department), Times.Once);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ArchiveDepartmentAsync_CountsInactiveEmployeesToo()
    {
        var department = Department.Create("Operasyon", null);
        var departmentRepository = new Mock<IDepartmentRepository>();
        departmentRepository
            .Setup(repository => repository.GetByIdAsync(department.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);
        var employeeRepository = new Mock<IEmployeeRepository>();
        employeeRepository
            .Setup(repository => repository.CountAllByDepartmentAsync(department.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var service = new DepartmentAppService(
            departmentRepository.Object,
            employeeRepository.Object,
            new Mock<IUnitOfWork>().Object);

        var act = () => service.SetStatusAsync(department.Id, new SetDepartmentStatusRequest(false));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*Aktif veya pasif çalışanı*");
        department.IsActive.Should().BeTrue();
    }

    private static Employee CreateEmployee() => Employee.Create(
        "Test Çalışanı",
        "test@example.com",
        "password-hash",
        null,
        "Uzman",
        ["Member"]);
}
