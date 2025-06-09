using ReadModelQuery.Core.Interfaces;
using Marten;

namespace ReadModelQuery.Core.Interfaces;

/// <summary>
/// Interface for query handlers
/// </summary>
public interface IQueryHandler<in TQuery> where TQuery : ISearchQuery
{
    Task<(IEnumerable<object> Results, int TotalCount)> HandleAsync(
        TQuery query, 
        Type documentType, 
        string? orderBy, 
        int skip, 
        int? take, 
        IDocumentSession session);
} 