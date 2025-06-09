using ReadModelQuery.Core.Interfaces;

namespace ReadModelQuery.Core.Services;

/// <summary>
/// Registry for managing query types
/// </summary>
public class QueryTypeRegistry : IQueryTypeRegistry
{
    private readonly Dictionary<string, Type> _queryTypes;

    public QueryTypeRegistry(IEnumerable<Type> queryTypes)
    {
        _queryTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var type in queryTypes)
        {
            if (typeof(ISearchQuery).IsAssignableFrom(type))
            {
                // Use the QueryType property value as the key
                var instance = Activator.CreateInstance(type) as ISearchQuery;
                if (instance != null)
                {
                    _queryTypes[instance.QueryType] = type;
                }
            }
        }
    }

    public Type GetQueryType(string queryTypeName)
    {
        if (_queryTypes.TryGetValue(queryTypeName, out var type))
        {
            return type;
        }
        
        throw new ArgumentException($"Unknown query type: {queryTypeName}. Available types: {string.Join(", ", _queryTypes.Keys)}");
    }

    public IEnumerable<Type> GetAllQueryTypes()
    {
        return _queryTypes.Values;
    }
} 