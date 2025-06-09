using ReadModelQuery.Core.Interfaces;
using ReadModelQuery.Features.SuperCoachPlayer.Models;
using ReadModelQuery.Features.SuperCoachPlayer.Queries;
using ReadModelQuery.Core.Extensions;
using Marten;

namespace ReadModelQuery.Features.SuperCoachPlayer.Handlers;

/// <summary>
/// Handler for PlayersByTeamQuery
/// </summary>
public class PlayersByTeamQueryHandler : IQueryHandler<PlayersByTeamQuery>
{
    public async Task<(IEnumerable<object> Results, int TotalCount)> HandleAsync(
        PlayersByTeamQuery query, 
        Type documentType, 
        string? orderBy, 
        int skip, 
        int? take, 
        IDocumentSession session)
    {
        if (documentType != typeof(SuperCoachPlayerDataContract))
            throw new ArgumentException($"Handler only supports {typeof(SuperCoachPlayerDataContract).Name}");

        var baseQuery = session.Query<SuperCoachPlayerDataContract>()
            .Where(p => p.TeamId == query.TeamId);

        if (query.Season.HasValue)
        {
            baseQuery = baseQuery.Where(p => p.Season == query.Season.Value);
        }

        var totalCount = await baseQuery.CountAsync();

        var orderedQuery = baseQuery.ApplyOrdering(orderBy);
        var pagedQuery = orderedQuery.Skip(skip);
        
        if (take.HasValue)
        {
            pagedQuery = pagedQuery.Take(take.Value);
        }

        var results = await pagedQuery.ToListAsync();
        return (results.Cast<object>(), totalCount);
    }
} 