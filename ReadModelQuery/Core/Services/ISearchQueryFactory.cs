using ReadModelQuery.Core.Interfaces;
using System.Text.Json;

namespace ReadModelQuery.Core.Services;

/// <summary>
/// Interface for creating search query instances
/// </summary>
public interface ISearchQueryFactory
{
    ISearchQuery CreateQuery(string queryType, JsonElement queryData);
} 