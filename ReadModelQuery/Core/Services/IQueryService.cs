using ReadModelQuery.Core.Interfaces;
using Marten;

namespace ReadModelQuery.Core.Services;

/// <summary>
/// Interface for executing queries
/// </summary>
public interface IQueryService
{
    Task<(IEnumerable<object> Results, int TotalCount)> ExecuteQueryAsync(
        ISearchQuery query, 
        Type documentType, 
        string? orderBy, 
        int skip, 
        int? take, 
        IDocumentSession session);
} 