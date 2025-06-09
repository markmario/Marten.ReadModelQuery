using ReadModelQuery.Core.Interfaces;
using ReadModelQuery.Core.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReadModelQuery.Core.Converters;

/// <summary>
/// JSON converter for ISearchQuery interface
/// </summary>
public class SearchQueryJsonConverter : JsonConverter<ISearchQuery>
{
    private readonly IQueryTypeRegistry _queryTypeRegistry;

    public SearchQueryJsonConverter(IQueryTypeRegistry queryTypeRegistry)
    {
        _queryTypeRegistry = queryTypeRegistry ?? throw new ArgumentNullException(nameof(queryTypeRegistry));
    }

    public override ISearchQuery Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("queryType", out var queryTypeElement))
        {
            throw new JsonException("Missing 'queryType' property in query object");
        }

        var queryTypeName = queryTypeElement.GetString();
        if (string.IsNullOrEmpty(queryTypeName))
        {
            throw new JsonException("'queryType' property cannot be null or empty");
        }

        var queryType = _queryTypeRegistry.GetQueryType(queryTypeName);
        var query = JsonSerializer.Deserialize(root.GetRawText(), queryType, options);
        
        if (query is not ISearchQuery searchQuery)
        {
            throw new JsonException($"Deserialized object is not an ISearchQuery: {queryType.Name}");
        }

        return searchQuery;
    }

    public override void Write(Utf8JsonWriter writer, ISearchQuery value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
} 