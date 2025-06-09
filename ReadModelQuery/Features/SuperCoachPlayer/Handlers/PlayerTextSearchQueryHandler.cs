using ReadModelQuery.Core.Interfaces;
using ReadModelQuery.Features.SuperCoachPlayer.Models;
using ReadModelQuery.Features.SuperCoachPlayer.Queries;
using ReadModelQuery.Core.Extensions;
using Marten;

namespace ReadModelQuery.Features.SuperCoachPlayer.Handlers;

/// <summary>
/// Handler for PlayerTextSearchQuery
/// </summary>
public class PlayerTextSearchQueryHandler : IQueryHandler<PlayerTextSearchQuery>
{
    public async Task<(IEnumerable<object> Results, int TotalCount)> HandleAsync(
        PlayerTextSearchQuery query, 
        Type documentType, 
        string? orderBy, 
        int skip, 
        int? take, 
        IDocumentSession session)
    {
        if (documentType != typeof(SuperCoachPlayerDataContract))
            throw new ArgumentException($"Handler only supports {typeof(SuperCoachPlayerDataContract).Name}");

        var searchTerm = query.SearchTerm.ToLower();
        var baseQuery = session.Query<SuperCoachPlayerDataContract>()
            .Where(p => 
                (p.FirstName != null && p.FirstName.ToLower().Contains(searchTerm)) ||
                (p.LastName != null && p.LastName.ToLower().Contains(searchTerm)) ||
                (p.FullName != null && p.FullName.ToLower().Contains(searchTerm)));

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