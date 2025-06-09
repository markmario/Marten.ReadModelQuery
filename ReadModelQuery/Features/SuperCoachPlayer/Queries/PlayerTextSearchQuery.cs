using ReadModelQuery.Core.Interfaces;

namespace ReadModelQuery.Features.SuperCoachPlayer.Queries;

/// <summary>
/// Well-known query for text search across players
/// </summary>
public sealed class PlayerTextSearchQuery : ISearchQuery
{
    public string QueryType => "PlayerTextSearch";
    public required string SearchTerm { get; set; }
} 