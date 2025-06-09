using System.Text.Json;
using System.Text.Json.Serialization;
using FastEndpoints;
using ReadModelQuery.Core.Interfaces;
using ReadModelQuery.Core.Services;
using ReadModelQuery.Core.Converters;

namespace ReadModelQuery.Core.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to register query handlers
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures FastEndpoints with custom JSON serialization and SearchQuery converter
    /// </summary>
    public static WebApplication UseFastEndpointsWithJsonConfig(this WebApplication app)
    {
        app.UseFastEndpoints(c =>
        {
            c.Serializer.Options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            c.Serializer.Options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            
            // Add our custom converter
            var queryTypeRegistry = app.Services.GetRequiredService<IQueryTypeRegistry>();
            var searchQueryConverter = new SearchQueryJsonConverter(queryTypeRegistry);
            c.Serializer.Options.Converters.Add(searchQueryConverter);
        });

        return app;
    }
} 