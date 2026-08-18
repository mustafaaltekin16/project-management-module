using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.AIGatewayService.Domain;

/// <summary>
/// One activity within an AI-suggested work package (<see cref="AiSuggestionItem"/>) — once the
/// package is approved, the package itself becomes a main task and each of its activities becomes a
/// sub-task under it (see WorkPackageApprovedConsumer in TaskService).
/// </summary>
public class AiSuggestionActivity : BaseEntity
{
    public Guid ItemId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public int? EffortHours { get; private set; }

    private AiSuggestionActivity() { }

    public AiSuggestionActivity(Guid itemId, string title, int? effortHours)
    {
        ItemId = itemId;
        Title = title;
        EffortHours = effortHours;
    }
}
