using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ozdilek.PM.BuildingBlocks.Auth;
using Ozdilek.PM.Contracts.Web;
using Ozdilek.PM.UserDirectoryService.Application.Dtos;
using Ozdilek.PM.UserDirectoryService.Application.Services;

namespace Ozdilek.PM.UserDirectoryService.Api.Controllers;

public sealed record LoginResponse(string AccessToken, Guid EmployeeId, string DisplayName, IReadOnlyList<string> Roles);

/// <summary>
/// This module is managed standalone — there is no corporate SSO to lean on — so it owns real sign-in
/// against the employee directory (email + password), not just token validation.
/// </summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController(AuthAppService authAppService, AuthOptions authOptions) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(LoginRequest request, CancellationToken ct)
    {
        var employee = await authAppService.VerifyCredentialsAsync(request, ct);
        var token = JwtTokenFactory.CreateToken(authOptions, employee.Id.ToString(), employee.DisplayName, employee.Roles, TimeSpan.FromHours(8));

        return Ok(ApiResponse<LoginResponse>.Ok(new LoginResponse(token, employee.Id, employee.DisplayName, employee.Roles.ToList())));
    }
}
