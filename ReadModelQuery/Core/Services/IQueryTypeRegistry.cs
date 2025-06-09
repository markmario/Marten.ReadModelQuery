namespace ReadModelQuery.Core.Services;

/// <summary>
/// Interface for managing query type registrations
/// </summary>
public interface IQueryTypeRegistry
{
    Type GetQueryType(string queryTypeName);
    IEnumerable<Type> GetAllQueryTypes();
} 