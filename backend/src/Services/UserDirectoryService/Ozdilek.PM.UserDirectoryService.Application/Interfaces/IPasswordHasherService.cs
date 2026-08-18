namespace Ozdilek.PM.UserDirectoryService.Application.Interfaces;

public interface IPasswordHasherService
{
    string Hash(string password);
    bool Verify(string passwordHash, string suppliedPassword);
}
