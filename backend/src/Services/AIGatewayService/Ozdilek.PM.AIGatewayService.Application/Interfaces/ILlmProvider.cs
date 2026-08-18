namespace Ozdilek.PM.AIGatewayService.Application.Interfaces;

/// <summary>
/// The LLM Abstraction Layer. The real implementation (<c>RagLlmProvider</c>) generates work packages
/// through the same self-hosted RAG service already used for document Q&amp;A — no paid external LLM API
/// call. <c>MockLlmProvider</c> remains for offline dev/tests. Swapping providers is a DI/config change
/// (see DependencyInjection.cs), never an application-code change.
/// </summary>
public interface ILlmProvider
{
    /// <summary>Name shown in audit logs / persisted on the suggestion request, e.g. "RAG", "Mock".</summary>
    string Name { get; }

    /// <summary>Sends the (already redacted) prompt to the model and returns its raw text response, expected to be a JSON array of work packages.</summary>
    Task<string> GenerateWorkPackagesJsonAsync(string prompt, CancellationToken ct = default);
}
