using ReadModelQuery.Core.Interfaces;
using ReadModelQuery.Features.SuperCoachPlayer.Models;
using ReadModelQuery.Features.SuperCoachPlayer.Queries;
using ReadModelQuery.Core.Extensions;
using Marten;

namespace ReadModelQuery.Features.SuperCoachPlayer.Handlers;

/// <summary>
/// Handler for PlayersByRoundPerformanceQuery
/// </summary>
public class PlayersByRoundPerformanceQueryHandler : IQueryHandler<PlayersByRoundPerformanceQuery>
{
    public async Task<(IEnumerable<object> Results, int TotalCount)> HandleAsync(
        PlayersByRoundPerformanceQuery query, 
        Type documentType, 
        string? orderBy, 
        int skip, 
        int? take, 
        IDocumentSession session)
    {
        if (documentType != typeof(SuperCoachPlayerDataContract))
            throw new ArgumentException($"Handler only supports {typeof(SuperCoachPlayerDataContract).Name}");

        var baseQuery = session.Query<SuperCoachPlayerDataContract>()
            .Where(p => p.Round == query.Round);

        if (query.MinPoints.HasValue)
        {
            baseQuery = baseQuery.Where(p => p.LastRoundScore >= query.MinPoints.Value);
        }

        if (query.MaxPoints.HasValue)
        {
            baseQuery = baseQuery.Where(p => p.LastRoundScore <= query.MaxPoints.Value);
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