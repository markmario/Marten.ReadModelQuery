using FastEndpoints;
using FastEndpoints.Swagger;
using Marten;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using ReadModelQuery.Core.Interfaces;
using ReadModelQuery.Core.Services;
using ReadModelQuery.Core.Converters;
using ReadModelQuery.Core.ModelBinding;
using ReadModelQuery.Core.Extensions;
using ReadModelQuery.Features.SuperCoachPlayer.Models;
using ReadModelQuery.Features.SuperCoachPlayer.Configuration;

namespace ReadModelQuery;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Register custom services first so they're available for other registrations
        builder.Services.AddSingleton<IQueryTypeRegistry>(provider =>
        {
            var assemblies = new[] { Assembly.GetExecutingAssembly() };
            var queryTypes = assemblies.SelectMany(assembly => 
                assembly.GetTypes()
                    .Where(type => !type.IsAbstract && !type.IsInterface)
                    .Where(type => typeof(ISearchQuery).IsAssignableFrom(type)))
                .ToList();
            return new QueryTypeRegistry(queryTypes);
        });

        builder.Services.AddSingleton<IDocumentTypeResolver, DocumentTypeResolver>();
        builder.Services.AddSingleton<ISearchQueryFactory, SearchQueryFactory>();
        
        // Register the JSON converter with access to the type registry
        builder.Services.AddSingleton<SearchQueryJsonConverter>();

        // Add model binding for complex query objects
        builder.Services.AddControllers(options =>
        {
            options.ModelBinderProviders.Insert(0, new SearchQueryModelBinderProvider());
        });

        // Configure Marten - the feature modules will register their document configurations
        builder.Services.AddMarten(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                ?? "[REDACTED]";
            
            options.Connection(connectionString);

            // Schema creation can be handled separately
            // For development, you may need to run migrations manually
        }).UseLightweightSessions(); // Explicitly specify lightweight sessions for better performance

        // Register feature modules
        builder.Services.AddSuperCoachPlayerFeature();

        // Register FastEndpoints
        builder.Services.AddFastEndpoints();

        builder.Services.AddSingleton<IQueryService, QueryService>();

        // Add Swagger
        builder.Services.SwaggerDocument(o =>
        {
            o.DocumentSettings = s =>
            {
                s.Title = "LBS Foundry ReadModel API";
                s.Version = "v1";
                s.Description = "API for querying read model documents using well-known queries";
            };
        });

        // Add logging
        builder.Services.AddLogging();

        var app = builder.Build();

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwaggerGen();
        }

        app.UseHttpsRedirection();
        
        // Configure FastEndpoints with custom JSON serialization
        app.UseFastEndpointsWithJsonConfig();

        // Seed sample data in development
        if (app.Environment.IsDevelopment())
        {
            SeedSampleData(app.Services);
        }

        app.Run();
    }

    /// <summary>
    /// Seeds sample data for development/testing
    /// </summary>
    private static void SeedSampleData(IServiceProvider services)
    {
        try
        {
            using var scope = services.CreateScope();
            using var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();

            // Check if data already exists
            var existingCount = session.Query<SuperCoachPlayerDataContract>().Count();
            if (existingCount > 0)
            {
                return; // Data already seeded
            }

            // Create sample players
            var samplePlayers = new[]
            {
                new SuperCoachPlayerDataContract
                {
                    PlayerId = 1,
                    FirstName = "James",
                    LastName = "Tedesco",
                    FullName = "James Tedesco",
                    Position = "FB",
                    TeamId = 1,
                    TeamShortName = "WST",
                    CurrentPrice = 650000,
                    LastRoundScore = 85,
                    TotalPoints = 1200,
                    AveragePoints = 75.5m,
                    Season = 2025,
                    Round = 14,
                    JerseyNumber = 1
                },
                new SuperCoachPlayerDataContract
                {
                    PlayerId = 2,
                    FirstName = "Nathan",
                    LastName = "Cleary",
                    FullName = "Nathan Cleary",
                    Position = "HLF",
                    TeamId = 2,
                    TeamShortName = "PEN",
                    CurrentPrice = 720000,
                    LastRoundScore = 92,
                    TotalPoints = 1350,
                    AveragePoints = 82.1m,
                    Season = 2025,
                    Round = 14,
                    JerseyNumber = 7
                },
                new SuperCoachPlayerDataContract
                {
                    PlayerId = 3,
                    FirstName = "Daly",
                    LastName = "Cherry-Evans",
                    FullName = "Daly Cherry-Evans",
                    Position = "HLF",
                    TeamId = 3,
                    TeamShortName = "MAN",
                    CurrentPrice = 680000,
                    LastRoundScore = 78,
                    TotalPoints = 1150,
                    AveragePoints = 71.2m,
                    Season = 2025,
                    Round = 14,
                    JerseyNumber = 7
                }
            };

            session.Store(samplePlayers);
            session.SaveChangesAsync().GetAwaiter().GetResult();

            Console.WriteLine($"Seeded {samplePlayers.Length} sample players");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding sample data: {ex.Message}");
        }
    }
} 