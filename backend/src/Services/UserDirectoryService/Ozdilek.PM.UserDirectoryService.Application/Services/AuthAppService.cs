using Ozdilek.PM.SharedKernel.Exceptions;
using Ozdilek.PM.UserDirectoryService.Application.Dtos;
using Ozdilek.PM.UserDirectoryService.Application.Interfaces;
using Ozdilek.PM.UserDirectoryService.Domain;

namespace Ozdilek.PM.UserDirectoryService.Application.Services;

/// <summary>
/// Verifies login credentials against the employee directory. Deliberately does not mint the JWT
/// itself — that needs AuthOptions/JwtTokenFactory from BuildingBlocks, which this framework-agnostic
/// layer doesn't reference. AuthController does the minting once credentials are verified here.
/// </summary>
public sealed class AuthAppService(IEmployeeRepository employees, IPasswordHasherService passwordHasher)
{
    public async Task<Employee> VerifyCredentialsAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new AuthenticationFailedException("E-posta ve şifre zorunludur.");
        }

        var employee = await employees.GetByEmailAsync(request.Email, ct);
        if (employee is null || !passwordHasher.Verify(employee.PasswordHash, request.Password))
        {
            throw new AuthenticationFailedException("E-posta veya şifre hatalı.");
        }

        return employee;
    }
}
