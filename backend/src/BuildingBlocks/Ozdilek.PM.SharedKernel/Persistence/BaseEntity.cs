namespace Ozdilek.PM.SharedKernel.Persistence;

/// <summary>Base type for every aggregate/entity so repositories and audit columns stay consistent across services.</summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; protected set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAtUtc { get; protected set; }

    public void MarkUpdated() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
