# Adding New Queries to ReadModelQuery System

This document explains how to add new query types to the ReadModelQuery system. The system uses a well-known query pattern with automatic discovery and dependency injection.

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Step-by-Step Guide](#step-by-step-guide)
- [Example: Adding a New Query](#example-adding-a-new-query)
- [Creating a New Vertical Slice](#creating-a-new-vertical-slice)
- [Advanced Scenarios](#advanced-scenarios)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Overview

The ReadModelQuery system is built around these core concepts:

- **Well-Known Queries**: Predefined query types with specific contracts
- **Query Handlers**: Classes that execute the query logic
- **Automatic Discovery**: Query types are automatically discovered and registered
- **Type Safety**: Strong typing ensures compile-time safety
- **Dependency Injection**: All components use DI for loose coupling

## Architecture

```
Client Request → FastEndpoint → QueryService → QueryHandler → Marten → Database
                      ↓
              Query Type Registry
                      ↓
              Document Type Resolver
```

### Core Components

1. **ISearchQuery**: Base interface for all queries
2. **IQueryHandler<TQuery>**: Generic interface for query handlers
3. **QueryTypeRegistry**: Manages query type registration
4. **QueryService**: Orchestrates query execution
5. **DocumentTypeResolver**: Maps data type strings to .NET types

## Step-by-Step Guide

### 1. Create a Query Class

Create a new query class that implements `ISearchQuery`:

```csharp
using ReadModelQuery.Core.Interfaces;

namespace ReadModelQuery.Features.YourFeature.Queries;

/// <summary>
/// Well-known query for [describe your query purpose]
/// </summary>
public sealed class YourNewQuery : ISearchQuery
{
    public string QueryType => "YourNewQuery"; // Unique identifier
    
    // Add your query parameters
    public required string SomeParameter { get; set; }
    public int? OptionalParameter { get; set; }
    public DateTime? DateFilter { get; set; }
}
```

**Important Notes:**
- Use `sealed` classes for queries
- `QueryType` property must be unique across all queries
- Use `required` for mandatory parameters
- Use nullable types for optional parameters
- Follow naming convention: `{Domain}{Action}Query`

### 2. Create a Query Handler

Create a handler that implements `IQueryHandler<TQuery>`:

```csharp
using ReadModelQuery.Core.Interfaces;
using ReadModelQuery.Features.YourFeature.Models;
using ReadModelQuery.Features.YourFeature.Queries;
using ReadModelQuery.Core.Extensions;
using Marten;

namespace ReadModelQuery.Features.YourFeature.Handlers;

/// <summary>
/// Handler for YourNewQuery
/// </summary>
public class YourNewQueryHandler : IQueryHandler<YourNewQuery>
{
    public async Task<(IEnumerable<object> Results, int TotalCount)> HandleAsync(
        YourNewQuery query, 
        Type documentType, 
        string? orderBy, 
        int skip, 
        int? take, 
        IDocumentSession session)
    {
        // 1. Validate document type
        if (documentType != typeof(YourDocumentType))
            throw new ArgumentException($"Handler only supports {typeof(YourDocumentType).Name}");

        // 2. Build base query
        var baseQuery = session.Query<YourDocumentType>();

        // 3. Apply filters based on query parameters
        if (!string.IsNullOrEmpty(query.SomeParameter))
        {
            baseQuery = baseQuery.Where(x => x.SomeProperty.Contains(query.SomeParameter));
        }

        if (query.OptionalParameter.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.NumericProperty >= query.OptionalParameter.Value);
        }

        if (query.DateFilter.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.DateProperty >= query.DateFilter.Value);
        }

        // 4. Get total count before pagination
        var totalCount = await baseQuery.CountAsync();

        // 5. Apply ordering (use extension method for consistency)
        var orderedQuery = baseQuery.ApplyOrdering(orderBy);

        // 6. Apply pagination
        var pagedQuery = orderedQuery.Skip(skip);
        if (take.HasValue)
        {
            pagedQuery = pagedQuery.Take(take.Value);
        }

        // 7. Execute query and return results
        var results = await pagedQuery.ToListAsync();
        return (results.Cast<object>(), totalCount);
    }
}
```

### 3. Register the Handler

Add the handler registration to your feature's service extension:

```csharp
// In Features/YourFeature/Configuration/YourFeatureServiceExtensions.cs
public static class YourFeatureServiceExtensions
{
    public static IServiceCollection AddYourFeature(this IServiceCollection services)
    {
        // Register Marten configuration if needed
        services.AddSingleton<IConfigureMarten, YourFeatureMartenModule>();
        
        // Register query handlers
        services.AddScoped<IQueryHandler<YourNewQuery>, YourNewQueryHandler>();
        
        return services;
    }
}
```

### 4. Register Feature in Program.cs

Add your feature registration to `Program.cs`:

```csharp
// In Program.cs, after other feature registrations
builder.Services.AddYourFeature();
```

### 5. Update Document Type Resolver (if needed)

If you're introducing a new document type, update `DocumentTypeResolver`:

```csharp
// In Core/Services/DocumentTypeResolver.cs
public DocumentTypeResolver()
{
    _typeMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
    {
        { "SuperCoachPlayer", typeof(SuperCoachPlayerDataContract) },
        { "YourNewDocumentType", typeof(YourNewDocumentType) }, // Add this line
        // ... other mappings
    };
}
```

## Example: Adding a New Query

Let's add a query to find players by injury status:

### 1. Create the Query

```csharp
// Features/SuperCoachPlayer/Queries/PlayersByInjuryStatusQuery.cs
using ReadModelQuery.Core.Interfaces;

namespace ReadModelQuery.Features.SuperCoachPlayer.Queries;

/// <summary>
/// Well-known query for SuperCoach players by injury status
/// </summary>
public sealed class PlayersByInjuryStatusQuery : ISearchQuery
{
    public string QueryType => "PlayersByInjuryStatus";
    public required string InjuryStatus { get; set; }
    public int? Season { get; set; }
    public int? TeamId { get; set; }
}
```

### 2. Create the Handler

```csharp
// Features/SuperCoachPlayer/Handlers/PlayersByInjuryStatusQueryHandler.cs
using ReadModelQuery.Core.Interfaces;
using ReadModelQuery.Features.SuperCoachPlayer.Models;
using ReadModelQuery.Features.SuperCoachPlayer.Queries;
using ReadModelQuery.Core.Extensions;
using Marten;

namespace ReadModelQuery.Features.SuperCoachPlayer.Handlers;

/// <summary>
/// Handler for PlayersByInjuryStatusQuery
/// </summary>
public class PlayersByInjuryStatusQueryHandler : IQueryHandler<PlayersByInjuryStatusQuery>
{
    public async Task<(IEnumerable<object> Results, int TotalCount)> HandleAsync(
        PlayersByInjuryStatusQuery query, 
        Type documentType, 
        string? orderBy, 
        int skip, 
        int? take, 
        IDocumentSession session)
    {
        if (documentType != typeof(SuperCoachPlayerDataContract))
            throw new ArgumentException($"Handler only supports {typeof(SuperCoachPlayerDataContract).Name}");

        var baseQuery = session.Query<SuperCoachPlayerDataContract>()
            .Where(p => p.InjuryStatus == query.InjuryStatus);

        if (query.Season.HasValue)
        {
            baseQuery = baseQuery.Where(p => p.Season == query.Season.Value);
        }

        if (query.TeamId.HasValue)
        {
            baseQuery = baseQuery.Where(p => p.TeamId == query.TeamId.Value);
        }

        var totalCount = await baseQuery.CountAsync();

        var orderedQuery = baseQuery.ApplyOrdering(orderBy);
        var pagedQuery = orderedQuery.Skip(skip);
        
        if (take.HasValue)
        {
            pagedQuery = pagedQuery.Take(take.Value);
        }

        var results = await pagedQuery.ToListAsync();
        return (results.Cast<object>(), totalCount);
    }
}
```

### 3. Register the Handler

```csharp
// Features/SuperCoachPlayer/Configuration/SuperCoachPlayerServiceExtensions.cs
public static IServiceCollection AddSuperCoachPlayerFeature(this IServiceCollection services)
{
    // ... existing registrations
    
    // Add new handler
    services.AddScoped<IQueryHandler<PlayersByInjuryStatusQuery>, PlayersByInjuryStatusQueryHandler>();
    
    return services;
}
```

### 4. Usage Examples

**JSON Request:**
```json
{
  "dataType": "SuperCoachPlayer",
  "query": {
    "queryType": "PlayersByInjuryStatus",
    "injuryStatus": "Fit",
    "season": 2025,
    "teamId": 1
  },
  "orderBy": "LastName ASC",
  "skip": 0,
  "take": 10
}
```

**Query String:**
```
GET /api/query?queryType=PlayersByInjuryStatus&injuryStatus=Fit&season=2025&teamId=1&dataType=SuperCoachPlayer&orderBy=LastName%20ASC&skip=0&take=10
```

## Creating a New Vertical Slice

A vertical slice represents a complete feature domain with its own models, queries, handlers, and configuration. Here's how to create an entirely new feature from scratch.

### Example: Adding a "Team" Feature

Let's create a complete vertical slice for managing team data.

### 1. Create the Folder Structure

```
ReadModelQuery/
└── Features/
    └── Team/
        ├── Models/
        ├── Queries/
        ├── Handlers/
        └── Configuration/
```

### 2. Create the Document Model

```csharp
// Features/Team/Models/TeamDataContract.cs
namespace ReadModelQuery.Features.Team.Models;

/// <summary>
/// Data contract for team information
/// </summary>
public class TeamDataContract
{
    public int TeamId { get; set; }
    public required string TeamName { get; set; }
    public required string ShortName { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? HomeGround { get; set; }
    public int? Capacity { get; set; }
    public DateTime? FoundedDate { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public bool IsActive { get; set; }
    public int? Season { get; set; }
    public string? Conference { get; set; }
    public object? Metadata { get; set; }
}
```

### 3. Create Marten Configuration

```csharp
// Features/Team/Configuration/TeamMartenModule.cs
using Marten;
using ReadModelQuery.Features.Team.Models;

namespace ReadModelQuery.Features.Team.Configuration;

/// <summary>
/// Marten configuration module for Team feature
/// </summary>
public class TeamMartenModule : IConfigureMarten
{
    public void Configure(IServiceProvider services, StoreOptions options)
    {
        // Register Team document type
        options.RegisterDocumentType<TeamDataContract>();
        
        // Configure document storage for Team
        options.Schema.For<TeamDataContract>()
            .Identity(x => x.TeamId)
            .Index(x => x.ShortName)
            .Index(x => x.City)
            .Index(x => x.State)
            .Index(x => x.Season)
            .Index(x => x.IsActive);
    }
}
```

### 4. Create Query Classes

```csharp
// Features/Team/Queries/TeamsByConferenceQuery.cs
using ReadModelQuery.Core.Interfaces;

namespace ReadModelQuery.Features.Team.Queries;

/// <summary>
/// Well-known query for teams by conference
/// </summary>
public sealed class TeamsByConferenceQuery : ISearchQuery
{
    public string QueryType => "TeamsByConference";
    public required string Conference { get; set; }
    public int? Season { get; set; }
    public bool? IsActive { get; set; }
}
```

```csharp
// Features/Team/Queries/TeamsByLocationQuery.cs
using ReadModelQuery.Core.Interfaces;

namespace ReadModelQuery.Features.Team.Queries;

/// <summary>
/// Well-known query for teams by location
/// </summary>
public sealed class TeamsByLocationQuery : ISearchQuery
{
    public string QueryType => "TeamsByLocation";
    public string? City { get; set; }
    public string? State { get; set; }
    public int? MinCapacity { get; set; }
    public int? MaxCapacity { get; set; }
}
```

```csharp
// Features/Team/Queries/TeamTextSearchQuery.cs
using ReadModelQuery.Core.Interfaces;

namespace ReadModelQuery.Features.Team.Queries;

/// <summary>
/// Well-known query for text search across teams
/// </summary>
public sealed class TeamTextSearchQuery : ISearchQuery
{
    public string QueryType => "TeamTextSearch";
    public required string SearchTerm { get; set; }
    public bool? IsActive { get; set; }
}
```

### 5. Create Query Handlers

```csharp
// Features/Team/Handlers/TeamsByConferenceQueryHandler.cs
using ReadModelQuery.Core.Interfaces;
using ReadModelQuery.Features.Team.Models;
using ReadModelQuery.Features.Team.Queries;
using ReadModelQuery.Core.Extensions;
using Marten;

namespace ReadModelQuery.Features.Team.Handlers;

/// <summary>
/// Handler for TeamsByConferenceQuery
/// </summary>
public class TeamsByConferenceQueryHandler : IQueryHandler<TeamsByConferenceQuery>
{
    public async Task<(IEnumerable<object> Results, int TotalCount)> HandleAsync(
        TeamsByConferenceQuery query, 
        Type documentType, 
        string? orderBy, 
        int skip, 
        int? take, 
        IDocumentSession session)
    {
        if (documentType != typeof(TeamDataContract))
            throw new ArgumentException($"Handler only supports {typeof(TeamDataContract).Name}");

        var baseQuery = session.Query<TeamDataContract>()
            .Where(t => t.Conference == query.Conference);

        if (query.Season.HasValue)
        {
            baseQuery = baseQuery.Where(t => t.Season == query.Season.Value);
        }

        if (query.IsActive.HasValue)
        {
            baseQuery = baseQuery.Where(t => t.IsActive == query.IsActive.Value);
        }

        var totalCount = await baseQuery.CountAsync();

        // Apply ordering - you'll need to extend QueryExtensions for Team
        var orderedQuery = baseQuery.ApplyTeamOrdering(orderBy);
        var pagedQuery = orderedQuery.Skip(skip);
        
        if (take.HasValue)
        {
            pagedQuery = pagedQuery.Take(take.Value);
        }

        var results = await pagedQuery.ToListAsync();
        return (results.Cast<object>(), totalCount);
    }
}
```

```csharp
// Features/Team/Handlers/TeamsByLocationQueryHandler.cs
using ReadModelQuery.Core.Interfaces;
using ReadModelQuery.Features.Team.Models;
using ReadModelQuery.Features.Team.Queries;
using ReadModelQuery.Core.Extensions;
using Marten;

namespace ReadModelQuery.Features.Team.Handlers;

/// <summary>
/// Handler for TeamsByLocationQuery
/// </summary>
public class TeamsByLocationQueryHandler : IQueryHandler<TeamsByLocationQuery>
{
    public async Task<(IEnumerable<object> Results, int TotalCount)> HandleAsync(
        TeamsByLocationQuery query, 
        Type documentType, 
        string? orderBy, 
        int skip, 
        int? take, 
        IDocumentSession session)
    {
        if (documentType != typeof(TeamDataContract))
            throw new ArgumentException($"Handler only supports {typeof(TeamDataContract).Name}");

        var baseQuery = session.Query<TeamDataContract>();

        if (!string.IsNullOrEmpty(query.City))
        {
            baseQuery = baseQuery.Where(t => t.City == query.City);
        }

        if (!string.IsNullOrEmpty(query.State))
        {
            baseQuery = baseQuery.Where(t => t.State == query.State);
        }

        if (query.MinCapacity.HasValue)
        {
            baseQuery = baseQuery.Where(t => t.Capacity >= query.MinCapacity.Value);
        }

        if (query.MaxCapacity.HasValue)
        {
            baseQuery = baseQuery.Where(t => t.Capacity <= query.MaxCapacity.Value);
        }

        var totalCount = await baseQuery.CountAsync();

        var orderedQuery = baseQuery.ApplyTeamOrdering(orderBy);
        var pagedQuery = orderedQuery.Skip(skip);
        
        if (take.HasValue)
        {
            pagedQuery = pagedQuery.Take(take.Value);
        }

        var results = await pagedQuery.ToListAsync();
        return (results.Cast<object>(), totalCount);
    }
}
```

```csharp
// Features/Team/Handlers/TeamTextSearchQueryHandler.cs
using ReadModelQuery.Core.Interfaces;
using ReadModelQuery.Features.Team.Models;
using ReadModelQuery.Features.Team.Queries;
using ReadModelQuery.Core.Extensions;
using Marten;

namespace ReadModelQuery.Features.Team.Handlers;

/// <summary>
/// Handler for TeamTextSearchQuery
/// </summary>
public class TeamTextSearchQueryHandler : IQueryHandler<TeamTextSearchQuery>
{
    public async Task<(IEnumerable<object> Results, int TotalCount)> HandleAsync(
        TeamTextSearchQuery query, 
        Type documentType, 
        string? orderBy, 
        int skip, 
        int? take, 
        IDocumentSession session)
    {
        if (documentType != typeof(TeamDataContract))
            throw new ArgumentException($"Handler only supports {typeof(TeamDataContract).Name}");

        var searchTerm = query.SearchTerm.ToLower();
        var baseQuery = session.Query<TeamDataContract>()
            .Where(t => 
                (t.TeamName != null && t.TeamName.ToLower().Contains(searchTerm)) ||
                (t.ShortName != null && t.ShortName.ToLower().Contains(searchTerm)) ||
                (t.City != null && t.City.ToLower().Contains(searchTerm)));

        if (query.IsActive.HasValue)
        {
            baseQuery = baseQuery.Where(t => t.IsActive == query.IsActive.Value);
        }

        var totalCount = await baseQuery.CountAsync();

        var orderedQuery = baseQuery.ApplyTeamOrdering(orderBy);
        var pagedQuery = orderedQuery.Skip(skip);
        
        if (take.HasValue)
        {
            pagedQuery = pagedQuery.Take(take.Value);
        }

        var results = await pagedQuery.ToListAsync();
        return (results.Cast<object>(), totalCount);
    }
}
```

### 6. Create Feature Service Extensions

```csharp
// Features/Team/Configuration/TeamServiceExtensions.cs
using Marten;
using ReadModelQuery.Features.Team.Handlers;
using ReadModelQuery.Features.Team.Queries;
using ReadModelQuery.Core.Interfaces;

namespace ReadModelQuery.Features.Team.Configuration;

/// <summary>
/// Extension methods for registering Team feature services
/// </summary>
public static class TeamServiceExtensions
{
    /// <summary>
    /// Registers all Team feature services including Marten configuration and query handlers
    /// </summary>
    public static IServiceCollection AddTeamFeature(this IServiceCollection services)
    {
        // Register the Marten configuration module
        services.AddSingleton<IConfigureMarten, TeamMartenModule>();
        
        // Register query handlers for this feature
        services.AddScoped<IQueryHandler<TeamsByConferenceQuery>, TeamsByConferenceQueryHandler>();
        services.AddScoped<IQueryHandler<TeamsByLocationQuery>, TeamsByLocationQueryHandler>();
        services.AddScoped<IQueryHandler<TeamTextSearchQuery>, TeamTextSearchQueryHandler>();
        
        return services;
    }
}
```

### 7. Extend QueryExtensions for Team Ordering

```csharp
// Add to Core/Extensions/QueryExtensions.cs
public static class QueryExtensions
{
    // ... existing SuperCoachPlayer methods ...

    /// <summary>
    /// Applies ordering to a team queryable based on orderBy string
    /// Format: "propertyName ASC|DESC, propertyName2 ASC|DESC"
    /// </summary>
    public static IQueryable<TeamDataContract> ApplyTeamOrdering(this IQueryable<TeamDataContract> queryable, string? orderBy)
    {
        if (string.IsNullOrWhiteSpace(orderBy))
        {
            return queryable.OrderBy(t => t.TeamName); // Default ordering
        }

        var orderClauses = orderBy.Split(',', StringSplitOptions.RemoveEmptyEntries);
        IOrderedQueryable<TeamDataContract>? orderedQueryable = null;

        for (int i = 0; i < orderClauses.Length; i++)
        {
            var clause = orderClauses[i].Trim();
            var parts = clause.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length == 0) continue;
            
            var propertyName = parts[0].ToLower();
            var isDescending = parts.Length > 1 && parts[1].ToUpper() == "DESC";

            if (i == 0) // First ordering clause
            {
                orderedQueryable = propertyName switch
                {
                    "teamname" => isDescending ? queryable.OrderByDescending(t => t.TeamName) : queryable.OrderBy(t => t.TeamName),
                    "shortname" => isDescending ? queryable.OrderByDescending(t => t.ShortName) : queryable.OrderBy(t => t.ShortName),
                    "city" => isDescending ? queryable.OrderByDescending(t => t.City) : queryable.OrderBy(t => t.City),
                    "state" => isDescending ? queryable.OrderByDescending(t => t.State) : queryable.OrderBy(t => t.State),
                    "capacity" => isDescending ? queryable.OrderByDescending(t => t.Capacity) : queryable.OrderBy(t => t.Capacity),
                    "foundeddate" => isDescending ? queryable.OrderByDescending(t => t.FoundedDate) : queryable.OrderBy(t => t.FoundedDate),
                    "conference" => isDescending ? queryable.OrderByDescending(t => t.Conference) : queryable.OrderBy(t => t.Conference),
                    "season" => isDescending ? queryable.OrderByDescending(t => t.Season) : queryable.OrderBy(t => t.Season),
                    "isactive" => isDescending ? queryable.OrderByDescending(t => t.IsActive) : queryable.OrderBy(t => t.IsActive),
                    _ => queryable.OrderBy(t => t.TeamName) // Default fallback
                };
            }
            else // Subsequent ordering clauses
            {
                orderedQueryable = propertyName switch
                {
                    "teamname" => isDescending ? orderedQueryable!.ThenByDescending(t => t.TeamName) : orderedQueryable!.ThenBy(t => t.TeamName),
                    "shortname" => isDescending ? orderedQueryable!.ThenByDescending(t => t.ShortName) : orderedQueryable!.ThenBy(t => t.ShortName),
                    "city" => isDescending ? orderedQueryable!.ThenByDescending(t => t.City) : orderedQueryable!.ThenBy(t => t.City),
                    "state" => isDescending ? orderedQueryable!.ThenByDescending(t => t.State) : orderedQueryable!.ThenBy(t => t.State),
                    "capacity" => isDescending ? orderedQueryable!.ThenByDescending(t => t.Capacity) : orderedQueryable!.ThenBy(t => t.Capacity),
                    "foundeddate" => isDescending ? orderedQueryable!.ThenByDescending(t => t.FoundedDate) : orderedQueryable!.ThenBy(t => t.FoundedDate),
                    "conference" => isDescending ? orderedQueryable!.ThenByDescending(t => t.Conference) : orderedQueryable!.ThenBy(t => t.Conference),
                    "season" => isDescending ? orderedQueryable!.ThenByDescending(t => t.Season) : orderedQueryable!.ThenBy(t => t.Season),
                    "isactive" => isDescending ? orderedQueryable!.ThenByDescending(t => t.IsActive) : orderedQueryable!.ThenBy(t => t.IsActive),
                    _ => orderedQueryable // No change for unknown properties
                };
            }
        }

        return orderedQueryable ?? queryable.OrderBy(t => t.TeamName);
    }
}
```

### 8. Update DocumentTypeResolver

```csharp
// Core/Services/DocumentTypeResolver.cs
public DocumentTypeResolver()
{
    _typeMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
    {
        { "SuperCoachPlayer", typeof(SuperCoachPlayerDataContract) },
        { "SuperCoachPlayerDataContract", typeof(SuperCoachPlayerDataContract) },
        { "Team", typeof(TeamDataContract) },
        { "TeamDataContract", typeof(TeamDataContract) }
    };
}
```

### 9. Register Feature in Program.cs

```csharp
// In Program.cs, after other feature registrations
builder.Services.AddSuperCoachPlayerFeature();
builder.Services.AddTeamFeature(); // Add this line
```

### 10. Add Sample Data (Optional)

```csharp
// Add to SeedSampleData method in Program.cs
private static void SeedSampleData(IServiceProvider services)
{
    try
    {
        using var scope = services.CreateScope();
        using var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();

        // Existing SuperCoachPlayer seeding...
        
        // Seed Team data
        var existingTeamCount = session.Query<TeamDataContract>().Count();
        if (existingTeamCount == 0)
        {
            var sampleTeams = new[]
            {
                new TeamDataContract
                {
                    TeamId = 1,
                    TeamName = "Western Sydney Tigers",
                    ShortName = "WST",
                    City = "Sydney",
                    State = "NSW",
                    HomeGround = "Leichhardt Oval",
                    Capacity = 20000,
                    FoundedDate = new DateTime(1908, 1, 1),
                    PrimaryColor = "Orange",
                    SecondaryColor = "Black",
                    IsActive = true,
                    Season = 2025,
                    Conference = "Eastern"
                },
                new TeamDataContract
                {
                    TeamId = 2,
                    TeamName = "Penrith Panthers",
                    ShortName = "PEN",
                    City = "Penrith",
                    State = "NSW",
                    HomeGround = "BlueBet Stadium",
                    Capacity = 22500,
                    FoundedDate = new DateTime(1967, 1, 1),
                    PrimaryColor = "Black",
                    SecondaryColor = "Pink",
                    IsActive = true,
                    Season = 2025,
                    Conference = "Eastern"
                }
            };

            session.Store(sampleTeams);
            Console.WriteLine($"Seeded {sampleTeams.Length} sample teams");
        }

        session.SaveChangesAsync().GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error seeding sample data: {ex.Message}");
    }
}
```

### 11. Usage Examples for New Vertical Slice

**JSON Request:**
```json
{
  "dataType": "Team",
  "query": {
    "queryType": "TeamsByConference",
    "conference": "Eastern",
    "season": 2025,
    "isActive": true
  },
  "orderBy": "TeamName ASC",
  "skip": 0,
  "take": 10
}
```

**Query String:**
```
GET /api/query?queryType=TeamTextSearch&searchTerm=Sydney&isActive=true&dataType=Team&orderBy=TeamName%20ASC&skip=0&take=10
```

### Vertical Slice Checklist

When creating a new vertical slice, ensure you complete all these steps:

- [ ] Create folder structure under `Features/{FeatureName}/`
- [ ] Create document model in `Models/`
- [ ] Create Marten configuration module
- [ ] Create query classes implementing `ISearchQuery`
- [ ] Create query handlers implementing `IQueryHandler<TQuery>`
- [ ] Create feature service extension class
- [ ] Extend `QueryExtensions` for ordering support
- [ ] Update `DocumentTypeResolver` with new document types
- [ ] Register feature in `Program.cs`
- [ ] Add sample data seeding (optional)
- [ ] Test all queries work correctly
- [ ] Update API documentation

This approach ensures complete feature isolation and follows the vertical slice architecture pattern.

## Advanced Scenarios

### Multi-Document Type Handlers

If your handler needs to support multiple document types:

```csharp
public async Task<(IEnumerable<object> Results, int TotalCount)> HandleAsync(
    YourQuery query, 
    Type documentType, 
    string? orderBy, 
    int skip, 
    int? take, 
    IDocumentSession session)
{
    if (documentType == typeof(DocumentTypeA))
    {
        return await HandleDocumentTypeA(query, orderBy, skip, take, session);
    }
    else if (documentType == typeof(DocumentTypeB))
    {
        return await HandleDocumentTypeB(query, orderBy, skip, take, session);
    }
    
    throw new ArgumentException($"Unsupported document type: {documentType.Name}");
}
```

### Complex Filtering Logic

For complex filtering scenarios:

```csharp
// Build query dynamically
var baseQuery = session.Query<YourDocumentType>();

// Apply filters conditionally
if (query.HasComplexFilter)
{
    baseQuery = baseQuery.Where(x => 
        x.Property1 > query.MinValue && 
        x.Property2.Contains(query.SearchTerm) &&
        query.AllowedStatuses.Contains(x.Status));
}

// Use Marten's advanced querying features
if (query.UseFullTextSearch)
{
    baseQuery = baseQuery.Where(x => x.PlainTextSearch(query.SearchTerm));
}
```

### Adding Validation

Add validation to your query classes:

```csharp
public sealed class YourQuery : ISearchQuery
{
    public string QueryType => "YourQuery";
    
    private string _searchTerm = string.Empty;
    public required string SearchTerm 
    { 
        get => _searchTerm;
        set 
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("SearchTerm cannot be empty");
            if (value.Length < 3)
                throw new ArgumentException("SearchTerm must be at least 3 characters");
            _searchTerm = value;
        }
    }
}
```

## Best Practices

### 1. Query Design
- Keep queries focused on a single responsibility
- Use descriptive names that clearly indicate the query purpose
- Make required parameters explicit with `required` keyword
- Use nullable types for optional parameters

### 2. Handler Implementation
- Always validate the document type
- Apply filters in a logical order (most selective first)
- Use the provided ordering extension methods
- Handle null/empty parameter values gracefully
- Return consistent result structures

### 3. Performance Considerations
- Index frequently queried properties in Marten configuration
- Avoid N+1 queries by using appropriate includes
- Consider using `CountAsync()` only when necessary for pagination
- Use `Take()` to limit result sets appropriately

### 4. Error Handling
- Provide clear error messages for validation failures
- Use appropriate exception types
- Log important query operations for debugging

### 5. Testing
- Write unit tests for query handlers
- Test edge cases (null parameters, empty results)
- Verify pagination works correctly
- Test ordering functionality

## Troubleshooting

### Common Issues

**1. "Unknown query type" Error**
- Ensure your query class implements `ISearchQuery`
- Verify the `QueryType` property returns a unique string
- Check that the assembly containing your query is being scanned

**2. "No handler registered" Error**
- Confirm your handler is registered in DI container
- Verify the handler implements `IQueryHandler<TYourQuery>`
- Check that your feature extension is called in `Program.cs`

**3. Serialization Issues**
- Ensure all query properties have public getters and setters
- Use `required` for mandatory properties
- Avoid complex object graphs in query parameters

**4. Ordering Not Working**
- Verify property names in `orderBy` match your document properties
- Check if `ApplyOrdering` extension supports your document type
- Ensure case-insensitive matching is working

### Debugging Tips

1. **Enable Debug Logging**: Set logging level to Debug to see query handler resolution
2. **Check Registration**: Verify services are registered by inspecting the DI container
3. **Test Query Serialization**: Test JSON serialization/deserialization manually
4. **Validate Marten Queries**: Use Marten's logging to see generated SQL

### Getting Help

- Check existing query implementations in the `Features` folder for patterns
- Review the core interfaces in the `Core/Interfaces` folder
- Look at test examples in the project for usage patterns
- Consult Marten documentation for advanced querying features

## File Structure Reference

```
ReadModelQuery/
├── Core/
│   ├── Interfaces/
│   │   ├── ISearchQuery.cs
│   │   ├── IQueryHandler.cs
│   │   └── IQueryTypeRegistry.cs
│   └── Services/
│       ├── QueryService.cs
│       ├── QueryTypeRegistry.cs
│       └── DocumentTypeResolver.cs
├── Features/
│   └── YourFeature/
│       ├── Queries/
│       │   └── YourNewQuery.cs
│       ├── Handlers/
│       │   └── YourNewQueryHandler.cs
│       ├── Models/
│       │   └── YourDocumentType.cs
│       └── Configuration/
│           ├── YourFeatureServiceExtensions.cs
│           └── YourFeatureMartenModule.cs
└── Program.cs
```

This structure ensures clean separation of concerns and makes it easy to add new features without affecting existing code. 