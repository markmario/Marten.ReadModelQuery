using ReadModelQuery.Features.SuperCoachPlayer.Models;

namespace ReadModelQuery.Core.Services;

/// <summary>
/// Resolves document types from string names
/// </summary>
public class DocumentTypeResolver : IDocumentTypeResolver
{
    private readonly Dictionary<string, Type> _typeMap;

    public DocumentTypeResolver()
    {
        _typeMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            { "SuperCoachPlayer", typeof(SuperCoachPlayerDataContract) },
            { "SuperCoachPlayerDataContract", typeof(SuperCoachPlayerDataContract) }
        };
    }

    public Type ResolveDocumentType(string dataType)
    {
        if (_typeMap.TryGetValue(dataType, out var type))
        {
            return type;
        }

        throw new ArgumentException($"Unknown data type: {dataType}. Available types: {string.Join(", ", _typeMap.Keys)}");
    }
} 