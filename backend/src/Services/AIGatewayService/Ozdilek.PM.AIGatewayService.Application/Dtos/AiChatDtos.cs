namespace Ozdilek.PM.AIGatewayService.Application.Dtos;

public sealed record AskProjectGuideRequestDto(Guid ProjectId, string Question);

public sealed record AskProjectGuideResponseDto(string Answer, IReadOnlyList<string> Sources, bool UsedRealDocumentContext);
