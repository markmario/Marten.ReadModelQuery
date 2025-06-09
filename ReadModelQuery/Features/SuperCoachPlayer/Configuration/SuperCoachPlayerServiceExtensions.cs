using Marten;
using ReadModelQuery.Features.SuperCoachPlayer.Handlers;
using ReadModelQuery.Features.SuperCoachPlayer.Queries;
using ReadModelQuery.Core.Interfaces;

namespace ReadModelQuery.Features.SuperCoachPlayer.Configuration;

/// <summary>
/// Extension methods for registering SuperCoachPlayer feature services
/// </summary>
public static class SuperCoachPlayerServiceExtensions
{
    /// <summary>
    /// Registers all SuperCoachPlayer feature services including Marten configuration and query handlers
    /// </summary>
    public static IServiceCollection AddSuperCoachPlayerFeature(this IServiceCollection services)
    {
        // Register the Marten configuration module
        services.AddSingleton<IConfigureMarten, SuperCoachPlayerMartenModule>();
        
        // Register query handlers for this feature
        services.AddScoped<IQueryHandler<PlayersByTeamQuery>, PlayersByTeamQueryHandler>();
        services.AddScoped<IQueryHandler<PlayersByPositionQuery>, PlayersByPositionQueryHandler>();
        services.AddScoped<IQueryHandler<PlayersByRoundPerformanceQuery>, PlayersByRoundPerformanceQueryHandler>();
        services.AddScoped<IQueryHandler<PlayerTextSearchQuery>, PlayerTextSearchQueryHandler>();
        
        return services;
    }
} 