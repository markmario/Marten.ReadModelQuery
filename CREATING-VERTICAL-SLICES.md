# Creating Vertical Slices in ReadModelQuery System

This document explains how to create complete new vertical slices (feature domains) in the ReadModelQuery system. A vertical slice represents a complete feature with its own models, queries, handlers, and configuration.

> **Related Documentation**: For adding queries to existing features, see [Adding New Queries](ADDING-NEW-QUERIES.md)

## Table of Contents

- [Overview](#overview)
- [What is a Vertical Slice?](#what-is-a-vertical-slice)
- [Architecture Principles](#architecture-principles)
- [Complete Example: Team Feature](#complete-example-team-feature)
- [Step-by-Step Guide](#step-by-step-guide)
- [Feature Integration](#feature-integration)
- [Best Practices](#best-practices)
- [Testing Your Vertical Slice](#testing-your-vertical-slice)
- [Troubleshooting](#troubleshooting)

## Overview

A vertical slice in the ReadModelQuery system represents a complete, self-contained feature domain that includes:

- **Document Models**: Data contracts representing your domain entities
- **Queries**: Well-known query types for your domain
- **Handlers**: Query execution logic
- **Configuration**: Marten setup and dependency injection
- **Extensions**: Supporting functionality like ordering

Each vertical slice is completely isolated and can be developed, tested, and deployed independently.

## What is a Vertical Slice?

Unlike horizontal layering (where you might have separate layers for data, business logic, and presentation), vertical slicing cuts through all layers for a specific feature:

```
Traditional Horizontal Layers:
┌─────────────────────────────────┐
│        Presentation Layer       │
├─────────────────────────────────┤
│        Business Logic Layer     │
├─────────────────────────────────┤
│        Data Access Layer        │
└─────────────────────────────────┘

Vertical Slice Approach:
┌───────────┬───────────┬───────────┐
│  Feature  │  Feature  │  Feature  │
│     A     │     B     │     C     │
│           │           │           │
│ ┌───────┐ │ ┌───────┐ │ ┌───────┐ │
│ │Queries│ │ │Queries│ │ │Queries│ │
│ │Models │ │ │Models │ │ │Models │ │
│ │Handler│ │ │Handler│ │ │Handler│ │
│ │Config │ │ │Config │ │ │Config │ │
│ └───────┘ │ └───────┘ │ └───────┘ │
└───────────┴───────────┴───────────┘
```

## Architecture Principles

### 1. Feature Isolation
- Each feature is completely self-contained
- No dependencies between features
- Can be developed by separate teams

### 2. Consistent Structure
- All features follow the same folder structure
- Common interfaces and patterns
- Predictable code organization

### 3. Independent Deployment
- Features can be enabled/disabled individually
- Database schemas are feature-specific
- Configuration is isolated

## Complete Example: Team Feature

Let's create a complete vertical slice for managing team data.

### Folder Structure

```
ReadModelQuery/
└── Features/
    └── Team/
        ├── Models/
        │   └── TeamDataContract.cs
        ├── Queries/
        │   ├── TeamsByConferenceQuery.cs
        │   ├── TeamsByLocationQuery.cs
        │   └── TeamTextSearchQuery.cs
        ├── Handlers/
        │   ├── TeamsByConferenceQueryHandler.cs
        │   ├── TeamsByLocationQueryHandler.cs
        │   └── TeamTextSearchQueryHandler.cs
        └── Configuration/
            ├── TeamMartenModule.cs
            └── TeamServiceExtensions.cs
```

## Step-by-Step Guide

### Step 1: Create the Document Model

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

### Step 2: Create Marten Configuration

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

### Step 3: Create Query Classes

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

### Step 4: Create Query Handlers

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

### Step 5: Create Feature Service Extensions

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

## Feature Integration

### Step 6: Extend QueryExtensions for Team Ordering

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

### Step 7: Update DocumentTypeResolver

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

### Step 8: Register Feature in Program.cs

```csharp
// In Program.cs, after other feature registrations
builder.Services.AddSuperCoachPlayerFeature();
builder.Services.AddTeamFeature(); // Add this line
```

### Step 9: Add Sample Data (Optional)

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

## Best Practices

### 1. Naming Conventions
- **Features**: Use domain-specific names (e.g., `Team`, `Player`, `Match`)
- **Queries**: Use descriptive action names (e.g., `TeamsByConference`, `PlayersWithInjuries`)
- **Handlers**: Match query names with `Handler` suffix
- **Models**: Use `DataContract` suffix for consistency

### 2. Query Design
- Keep queries focused and specific to your domain
- Use `required` for mandatory parameters
- Use nullable types for optional filters
- Provide clear documentation for each query

### 3. Handler Implementation
- Always validate document types
- Apply filters in logical order (most selective first)
- Use consistent error handling
- Implement proper pagination support

### 4. Database Design
- Create appropriate indexes for your query patterns
- Use meaningful identity fields
- Consider query performance when designing indexes

### 5. Testing Strategy
- Test each query handler independently
- Verify edge cases (empty results, null parameters)
- Test pagination and ordering
- Use sample data for integration testing

## Testing Your Vertical Slice

### 1. Unit Testing Query Handlers

```csharp
[Test]
public async Task TeamsByConferenceQuery_ShouldFilterByConference()
{
    // Arrange
    var query = new TeamsByConferenceQuery 
    { 
        Conference = "Eastern",
        Season = 2025 
    };
    
    // Act & Assert
    // Test your handler logic
}
```

### 2. Integration Testing

```csharp
[Test]
public async Task TeamFeature_ShouldBeRegisteredCorrectly()
{
    // Verify that all services are registered
    // Test actual query execution
    // Verify database interactions
}
```

### 3. API Testing

```json
// Test via HTTP requests
{
  "dataType": "Team",
  "query": {
    "queryType": "TeamsByConference",
    "conference": "Eastern",
    "season": 2025
  },
  "orderBy": "TeamName ASC",
  "skip": 0,
  "take": 10
}
```

## Troubleshooting

### Common Issues

**1. "Unknown data type" Error**
- Verify `DocumentTypeResolver` includes your new type
- Check spelling and casing in data type mappings

**2. "No handler registered" Error**
- Ensure handler is registered in your service extension
- Verify the feature extension is called in `Program.cs`

**3. Query Not Found**
- Check that query implements `ISearchQuery`
- Verify `QueryType` property returns correct string
- Ensure assembly is being scanned for queries

**4. Database/Index Issues**
- Verify Marten configuration is registered
- Check that indexes are created for your query patterns
- Review SQL logs for performance issues

### Debugging Tips

1. **Enable Debug Logging**: Set logging to Debug level to see handler resolution
2. **Check Service Registration**: Verify all services are registered in DI container
3. **Test Queries Independently**: Test each query handler in isolation
4. **Review Marten Logs**: Enable SQL logging to see generated queries

## Vertical Slice Checklist

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
- [ ] Write unit tests for handlers
- [ ] Write integration tests for the feature
- [ ] Test all queries work correctly via API
- [ ] Update API documentation

## Usage Examples

### JSON Request
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

### Query String
```
GET /api/query?queryType=TeamTextSearch&searchTerm=Sydney&isActive=true&dataType=Team&orderBy=TeamName%20ASC&skip=0&take=10
```

This approach ensures complete feature isolation and follows the vertical slice architecture pattern, making your system more maintainable and scalable.

---

> **Next Steps**: Once you've created your vertical slice, you might want to add additional queries to it. See [Adding New Queries](ADDING-NEW-QUERIES.md) for guidance on extending existing features. 