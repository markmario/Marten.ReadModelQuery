using ReadModelQuery.Core.Interfaces;

namespace ReadModelQuery.Features.SuperCoachPlayer.Queries;

/// <summary>
/// Well-known query for SuperCoach players by team
/// </summary>
public sealed class PlayersByTeamQuery : ISearchQuery
{
    public string QueryType => "PlayersByTeam";
    public int TeamId { get; set; }
    public int? Season { get; set; }
} 