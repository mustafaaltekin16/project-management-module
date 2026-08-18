namespace Ozdilek.PM.AIGatewayService.Application.Services;

/// <summary>
/// Binds the "Rag" configuration section — the external RAG (Retrieval Augmented Generation) service's
/// base URL and behavior defaults. Deliberately its own section, not folded into the "Services:*" cross-
/// microservice URL pattern: those are bare strings sharing this app's own JWT via
/// BearerTokenForwardingHandler, whereas RAG is a separate deployment with its own optional API-key auth
/// and several behavior knobs (mode, history, polling) that don't fit a single string value.
/// </summary>
public sealed class RagOptions
{
    public const string SectionName = "Rag";

    // Placeholder — the RAG service isn't deployed yet (will run on RunPod behind vLLM). Update via the
    // Rag__BaseUrl env var (see docker-compose.yml) once a real URL exists; no code change needed.
    public string BaseUrl { get; set; } = "http://localhost:8100";
    public string? ApiKey { get; set; }
    public string DefaultMode { get; set; } = "strict";
    // Deliberately false: chat and İş Paketi generation share one RAG session per project (see
    // RagDocumentSyncService), so turning history on would let one feature's/user's prior turns silently
    // steer another's next question. Revisit once RunPod is live and real per-session history semantics
    // are confirmed safe to enable (see the plan's open questions).
    public bool DefaultUseHistory { get; set; } = false;
    public int JobPollIntervalMs { get; set; } = 1500;
    // 20 saniyeydi — RunPod pod'u soğuk başladığında (30-90 sn gözlemlendi) indeksleme bu süreyi
    // bitiremeden poller pes edip RagLlmProvider'ın InvalidOperationException fırlatmasına yol açıyordu.
    public int JobPollTimeoutMs { get; set; } = 120000;
}
