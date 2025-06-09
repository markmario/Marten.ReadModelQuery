using ReadModelQuery.Core.Interfaces;

namespace ReadModelQuery.Features.SuperCoachPlayer.Queries;

/// <summary>
/// Well-known query for SuperCoach players by position
/// </summary>
public sealed class PlayersByPositionQuery : ISearchQuery
{
    public string QueryType => "PlayersByPosition";
    public required string Position { get; set; }
    public int? MinPrice { get; set; }
    public int? MaxPrice { get; set; }
} 