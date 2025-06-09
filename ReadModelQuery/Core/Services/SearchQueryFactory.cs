using ReadModelQuery.Core.Interfaces;
using System.Text.Json;

namespace ReadModelQuery.Core.Services;

/// <summary>
/// Factory for creating search query instances
/// </summary>
public class SearchQueryFactory : ISearchQueryFactory
{
    private readonly IQueryTypeRegistry _queryTypeRegistry;

    public SearchQueryFactory(IQueryTypeRegistry queryTypeRegistry)
    {
        _queryTypeRegistry = queryTypeRegistry ?? throw new ArgumentNullException(nameof(queryTypeRegistry));
    }

    public ISearchQuery CreateQuery(string queryType, JsonElement queryData)
    {
        var type = _queryTypeRegistry.GetQueryType(queryType);
        var query = JsonSerializer.Deserialize(queryData.GetRawText(), type);
        
        if (query is not ISearchQuery searchQuery)
        {
            throw new InvalidOperationException($"Deserialized object is not an ISearchQuery: {type.Name}");
        }

        return searchQuery;
    }
} 