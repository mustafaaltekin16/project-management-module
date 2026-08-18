using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Ozdilek.PM.AIGatewayService.Application.Dtos;
using Ozdilek.PM.AIGatewayService.Application.Interfaces;

namespace Ozdilek.PM.AIGatewayService.Application.Services;

/// <summary>
/// Shared RAG "upload is async, poll the job" wait loop — used by both <see cref="RagDocumentSyncService"/>
/// (real project documents) and <see cref="WorkPackageContextRetrievalService"/> (ephemeral synthetic
/// task/suggestion snapshots), so the timeout/interval logic lives in exactly one place.
/// </summary>
public static class RagJobPoller
{
    public static async Task<bool> PollUntilDoneAsync(
        IRagClient ragClient, RagOptions ragOptions, string jobId, string itemLabel,
        ILogger logger, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < ragOptions.JobPollTimeoutMs)
        {
            RagJobStatus? status;
            try
            {
                status = await ragClient.GetJobStatusAsync(jobId, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "{Label} için RAG iş durumu sorgulanamadı (job {JobId}).", itemLabel, jobId);
                return false;
            }

            if (status?.Status == "done")
            {
                return true;
            }
            if (status?.Status == "failed")
            {
                logger.LogWarning("{Label} RAG indekslemesi başarısız oldu (job {JobId}).", itemLabel, jobId);
                return false;
            }

            await Task.Delay(ragOptions.JobPollIntervalMs, ct);
        }

        logger.LogWarning(
            "{Label} için RAG indekslemesi {TimeoutMs}ms içinde tamamlanmadı (job {JobId}), zaman aşımına uğradı.",
            itemLabel, ragOptions.JobPollTimeoutMs, jobId);
        return false;
    }
}
