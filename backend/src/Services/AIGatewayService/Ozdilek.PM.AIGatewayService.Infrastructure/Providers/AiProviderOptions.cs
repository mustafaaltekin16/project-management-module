namespace Ozdilek.PM.AIGatewayService.Infrastructure.Providers;

/// <summary>
/// Binds the "Ai" configuration section. <see cref="Provider"/> selects which <c>ILlmProvider</c>
/// implementation is registered (see DependencyInjection.cs) — "RAG" (default, no paid external LLM API
/// call) or "Mock" (no network at all, for offline dev/tests). No paid external LLM provider (OpenAI/
/// Anthropic/Gemini) is wired anymore — work-package generation runs entirely through the same RAG
/// service already used for document Q&amp;A.
/// </summary>
public sealed class AiProviderOptions
{
    public const string SectionName = "Ai";

    public string Provider { get; set; } = "RAG";
}
