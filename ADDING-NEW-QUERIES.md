# Adding New Queries to ReadModelQuery System

This document explains how to add new query types to the ReadModelQuery system. The system uses a well-known query pattern with automatic discovery and dependency injection.

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Step-by-Step Guide](#step-by-step-guide)
- [Example: Adding a New Query](#example-adding-a-new-query)
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