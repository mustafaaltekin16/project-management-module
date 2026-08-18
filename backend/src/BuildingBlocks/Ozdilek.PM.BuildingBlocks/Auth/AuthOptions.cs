namespace Ozdilek.PM.BuildingBlocks.Auth;

/// <summary>
/// Binds the "Auth" configuration section. This module never issues production tokens itself —
/// it only validates JWTs. "ExternalOidc" points at the corporate identity provider (Azure AD / Entra,
/// Keycloak, an existing IdentityServer, ...). "Dev" validates tokens signed with a local symmetric key
/// so the module can be run and tested end-to-end before that provider is wired up.
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string Mode { get; set; } = "Dev";
    public string? Authority { get; set; }
    public string Audience { get; set; } = "cwa-project-management";
    public string DevSigningKey { get; set; } = "dev-only-signing-key-change-me-32-bytes-min";
    public bool EnableDevTokenIssuer { get; set; } = true;
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string ProjectManager = "ProjectManager";
    public const string Approver = "Approver";
    public const string Member = "Member";
}

public static class Policies
{
    public const string CanManageProjects = "CanManageProjects";
    public const string CanManageDirectory = "CanManageDirectory";
    public const string CanApprove = "CanApprove";
    // Deliberately narrower than CanManageProjects — irreversible actions (deleting a project outright)
    // are reserved for Admin, not the broader Admin+ProjectManager "day to day management" tier.
    public const string CanDeleteProjects = "CanDeleteProjects";
}
