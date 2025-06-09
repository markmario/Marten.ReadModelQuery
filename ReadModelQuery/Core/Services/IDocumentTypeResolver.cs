namespace ReadModelQuery.Core.Services;

/// <summary>
/// Interface for resolving document types from string names
/// </summary>
public interface IDocumentTypeResolver
{
    Type ResolveDocumentType(string dataType);
} 