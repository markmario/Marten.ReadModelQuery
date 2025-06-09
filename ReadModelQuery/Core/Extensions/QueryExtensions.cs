using ReadModelQuery.Features.SuperCoachPlayer.Models;

namespace ReadModelQuery.Core.Extensions;

/// <summary>
/// Extension methods for query manipulation
/// </summary>
public static class QueryExtensions
{
    /// <summary>
    /// Applies ordering to a queryable based on orderBy string
    /// Format: "propertyName ASC|DESC, propertyName2 ASC|DESC"
    /// </summary>
    public static IQueryable<SuperCoachPlayerDataContract> ApplyOrdering(this IQueryable<SuperCoachPlayerDataContract> queryable, string? orderBy)
    {
        if (string.IsNullOrWhiteSpace(orderBy))
        {
            return queryable.OrderBy(p => p.PlayerId); // Default ordering
        }

        var orderClauses = orderBy.Split(',', StringSplitOptions.RemoveEmptyEntries);
        IOrderedQueryable<SuperCoachPlayerDataContract>? orderedQueryable = null;

        for (int i = 0; i < orderClauses.Length; i++)
        {
            var clause = orderClauses[i].Trim();
            var parts = clause.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length == 0) continue;
            
            var propertyName = parts[0].ToLower();
            var isDescending = parts.Length > 1 && parts[1].ToUpper() == "DESC";

            if (i == 0)
            {
                orderedQueryable = propertyName switch
                {
                    "playerid" => isDescending ? queryable.OrderByDescending(p => p.PlayerId) : queryable.OrderBy(p => p.PlayerId),
                    "firstname" => isDescending ? queryable.OrderByDescending(p => p.FirstName) : queryable.OrderBy(p => p.FirstName),
                    "lastname" => isDescending ? queryable.OrderByDescending(p => p.LastName) : queryable.OrderBy(p => p.LastName),
                    "fullname" => isDescending ? queryable.OrderByDescending(p => p.FullName) : queryable.OrderBy(p => p.FullName),
                    "position" => isDescending ? queryable.OrderByDescending(p => p.Position) : queryable.OrderBy(p => p.Position),
                    "currentprice" => isDescending ? queryable.OrderByDescending(p => p.CurrentPrice) : queryable.OrderBy(p => p.CurrentPrice),
                    "teamid" => isDescending ? queryable.OrderByDescending(p => p.TeamId) : queryable.OrderBy(p => p.TeamId),
                    "teamshortname" => isDescending ? queryable.OrderByDescending(p => p.TeamShortName) : queryable.OrderBy(p => p.TeamShortName),
                    "totalpoints" => isDescending ? queryable.OrderByDescending(p => p.TotalPoints) : queryable.OrderBy(p => p.TotalPoints),
                    "averagepoints" => isDescending ? queryable.OrderByDescending(p => p.AveragePoints) : queryable.OrderBy(p => p.AveragePoints),
                    "lastroundscore" => isDescending ? queryable.OrderByDescending(p => p.LastRoundScore) : queryable.OrderBy(p => p.LastRoundScore),
                    "season" => isDescending ? queryable.OrderByDescending(p => p.Season) : queryable.OrderBy(p => p.Season),
                    "round" => isDescending ? queryable.OrderByDescending(p => p.Round) : queryable.OrderBy(p => p.Round),
                    _ => queryable.OrderBy(p => p.PlayerId) // Default fallback
                };
            }
            else if (orderedQueryable != null)
            {
                orderedQueryable = propertyName switch
                {
                    "playerid" => isDescending ? orderedQueryable.ThenByDescending(p => p.PlayerId) : orderedQueryable.ThenBy(p => p.PlayerId),
                    "firstname" => isDescending ? orderedQueryable.ThenByDescending(p => p.FirstName) : orderedQueryable.ThenBy(p => p.FirstName),
                    "lastname" => isDescending ? orderedQueryable.ThenByDescending(p => p.LastName) : orderedQueryable.ThenBy(p => p.LastName),
                    "fullname" => isDescending ? orderedQueryable.ThenByDescending(p => p.FullName) : orderedQueryable.ThenBy(p => p.FullName),
                    "position" => isDescending ? orderedQueryable.ThenByDescending(p => p.Position) : orderedQueryable.ThenBy(p => p.Position),
                    "currentprice" => isDescending ? orderedQueryable.ThenByDescending(p => p.CurrentPrice) : orderedQueryable.ThenBy(p => p.CurrentPrice),
                    "teamid" => isDescending ? orderedQueryable.ThenByDescending(p => p.TeamId) : orderedQueryable.ThenBy(p => p.TeamId),
                    "teamshortname" => isDescending ? orderedQueryable.ThenByDescending(p => p.TeamShortName) : orderedQueryable.ThenBy(p => p.TeamShortName),
                    "totalpoints" => isDescending ? orderedQueryable.ThenByDescending(p => p.TotalPoints) : orderedQueryable.ThenBy(p => p.TotalPoints),
                    "averagepoints" => isDescending ? orderedQueryable.ThenByDescending(p => p.AveragePoints) : orderedQueryable.ThenBy(p => p.AveragePoints),
                    "lastroundscore" => isDescending ? orderedQueryable.ThenByDescending(p => p.LastRoundScore) : orderedQueryable.ThenBy(p => p.LastRoundScore),
                    "season" => isDescending ? orderedQueryable.ThenByDescending(p => p.Season) : orderedQueryable.ThenBy(p => p.Season),
                    "round" => isDescending ? orderedQueryable.ThenByDescending(p => p.Round) : orderedQueryable.ThenBy(p => p.Round),
                    _ => orderedQueryable // No change for unknown properties
                };
            }
        }

        return orderedQueryable ?? queryable.OrderBy(p => p.PlayerId);
    }
} 