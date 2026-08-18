using Newtonsoft.Json;
using Ozdilek.PM.ProjectService.Application.Interfaces;
using Ozdilek.PM.SharedKernel.Exceptions;

namespace Ozdilek.PM.ProjectService.Infrastructure.Clients;

internal sealed class DirectoryEnvelope<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
}

internal sealed class DirectoryEmployeeResponse
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public List<string> Roles { get; set; } = [];
    public bool IsActive { get; set; }
    public bool IsSelectable { get; set; }
}

internal sealed class DirectoryDepartmentResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? HeadEmployeeId { get; set; }
    public bool IsActive { get; set; }
}

public sealed class UserDirectoryClient(HttpClient httpClient) : IUserDirectoryClient
{
    public async Task<DirectoryEmployee> GetEmployeeAsync(Guid id, CancellationToken ct = default)
    {
        var employee = await GetAsync<DirectoryEmployeeResponse>($"/api/employees/{id}", "Çalışan", ct);
        return new DirectoryEmployee(
            employee.Id,
            employee.DisplayName,
            employee.DepartmentId,
            employee.Roles,
            employee.IsActive,
            employee.IsSelectable);
    }

    public async Task<DirectoryDepartment> GetDepartmentAsync(Guid id, CancellationToken ct = default)
    {
        var department = await GetAsync<DirectoryDepartmentResponse>($"/api/departments/{id}", "Departman", ct);
        return new DirectoryDepartment(
            department.Id,
            department.Name,
            department.HeadEmployeeId,
            department.IsActive);
    }

    private async Task<T> GetAsync<T>(string path, string entityName, CancellationToken ct)
    {
        using var response = await httpClient.GetAsync(path, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new NotFoundException($"{entityName} bulunamadı.");
        }
        if (!response.IsSuccessStatusCode)
        {
            throw new DomainException($"{entityName} bilgisi doğrulanamadı.");
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        var envelope = JsonConvert.DeserializeObject<DirectoryEnvelope<T>>(body);
        return envelope is { Success: true, Data: not null }
            ? envelope.Data
            : throw new DomainException($"{entityName} bilgisi doğrulanamadı.");
    }
}
