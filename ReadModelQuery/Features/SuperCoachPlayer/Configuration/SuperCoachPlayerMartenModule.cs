using Marten;
using ReadModelQuery.Features.SuperCoachPlayer.Models;

namespace ReadModelQuery.Features.SuperCoachPlayer.Configuration;

/// <summary>
/// Marten configuration module for SuperCoachPlayer feature
/// </summary>
public class SuperCoachPlayerMartenModule : IConfigureMarten
{
    public void Configure(IServiceProvider services, StoreOptions options)
    {
        // Register SuperCoachPlayer document type
        options.RegisterDocumentType<SuperCoachPlayerDataContract>();
        
        // Configure document storage for SuperCoachPlayer
        options.Schema.For<SuperCoachPlayerDataContract>()
            .Identity(x => x.PlayerId)
            .Index(x => x.TeamId)
            .Index(x => x.Position)
            .Index(x => x.Season)
            .Index(x => x.Round);
    }
} 