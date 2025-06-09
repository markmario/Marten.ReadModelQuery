using ReadModelQuery.Core.Interfaces;

namespace ReadModelQuery.Features.SuperCoachPlayer.Queries;

/// <summary>
/// Well-known query for SuperCoach players by round performance
/// </summary>
public sealed class PlayersByRoundPerformanceQuery : ISearchQuery
{
    public string QueryType => "PlayersByRoundPerformance";
    public int Round { get; set; }
    public int? MinPoints { get; set; }
    public int? MaxPoints { get; set; }
} 