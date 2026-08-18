using Microsoft.AspNetCore.Identity;
using Ozdilek.PM.UserDirectoryService.Application.Interfaces;
using Ozdilek.PM.UserDirectoryService.Domain;

namespace Ozdilek.PM.UserDirectoryService.Infrastructure.Security;

/// <summary>Thin wrapper around ASP.NET Core's PBKDF2-based hasher — no separate Identity/EF store needed, just the hashing algorithm.</summary>
public sealed class PasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<Employee> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string passwordHash, string suppliedPassword) =>
        _hasher.VerifyHashedPassword(null!, passwordHash, suppliedPassword) != PasswordVerificationResult.Failed;
}
