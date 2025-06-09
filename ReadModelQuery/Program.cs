using FastEndpoints;
using FastEndpoints.Swagger;
using Marten;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LBS.Foundry.Api
{
    /// <summary>
    /// Interface for defining search queries
    /// </summary>
    public interface ISearchQuery
    {
        string QueryType { get; }
    }

    /// <summary>
    /// Well-known query for SuperCoach players by team
    /// </summary>
    public sealed class PlayersByTeamQuery : ISearchQuery
    {
        public string QueryType => "PlayersByTeam";
        public int TeamId { get; set; }
        public int? Season { get; set; }
    }

    /// <summary>
    /// Well-known query for SuperCoach players by position
    /// </summary>
    public sealed class PlayersByPositionQuery : ISearchQuery
    {
        public string QueryType => "PlayersByPosition";
        public required string Position { get; set; }
        public int? MinPrice { get; set; }
        public int? MaxPrice { get; set; }
    }

    /// <summary>
    /// Well-known query for SuperCoach players by round performance
    /// </summary>
    public sealed class PlayersByRoundPerformanceQuery : ISearchQuery
    {
        public string QueryType => "PlayersByRoundPerformance";
        public int Round { get; set; }
        public int? MinPoints { get; set; }
        public int? MaxPoints { get; set; }
    }

    /// <summary>
    /// Well-known query for text search across players
    /// </summary>
    public sealed class PlayerTextSearchQuery : ISearchQuery
    {
        public string QueryType => "PlayerTextSearch";
        public required string SearchTerm { get; set; }
    }

    /// <summary>
    /// Interface for query handlers
    /// </summary>
    public interface IQueryHandler<in TQuery> where TQuery : ISearchQuery
    {
        Task<(IEnumerable<object> Results, int TotalCount)> HandleAsync(
            TQuery query, 
            Type documentType, 
            string? orderBy, 
            int skip, 
            int? take, 
            IDocumentSession session);
    }

    /// <summary>
    /// Handler for players by team queries
    /// </summary>
    public class PlayersByTeamQueryHandler : IQueryHandler<PlayersByTeamQuery>
    {
        public async Task<(IEnumerable<object> Results, int TotalCount)> HandleAsync(
            PlayersByTeamQuery query, 
            Type documentType, 
            string? orderBy, 
            int skip, 
            int? take, 
            IDocumentSession session)
        {
            var queryable = session.Query<SuperCoachPlayerDataContract>()
                .Where(x => x.TeamId == query.TeamId);

            if (query.Season.HasValue)
            {
                queryable = queryable.Where(x => x.Season == query.Season.Value);
            }

            var totalCount = await queryable.CountAsync();

            var orderedQueryable = queryable.ApplyOrdering(orderBy);
            var results = await orderedQueryable
                .Skip(skip)
                .Take(take ?? 50)
                .ToListAsync();

            return (results.Cast<object>(), totalCount);
        }
    }

    /// <summary>
    /// Handler for players by position queries
    /// </summary>
    public class PlayersByPositionQueryHandler : IQueryHandler<PlayersByPositionQuery>
    {
        public async Task<(IEnumerable<object> Results, int TotalCount)> HandleAsync(
            PlayersByPositionQuery query, 
            Type documentType, 
            string? orderBy, 
            int skip, 
            int? take, 
            IDocumentSession session)
        {
            var queryable = session.Query<SuperCoachPlayerDataContract>()
                .Where(x => x.Position != null && x.Position.Contains(query.Position));

            if (query.MinPrice.HasValue)
            {
                queryable = queryable.Where(x => x.CurrentPrice >= query.MinPrice.Value);
            }

            if (query.MaxPrice.HasValue)
            {
                queryable = queryable.Where(x => x.CurrentPrice <= query.MaxPrice.Value);
            }

            var totalCount = await queryable.CountAsync();

            var orderedQueryable = queryable.ApplyOrdering(orderBy);
            var results = await orderedQueryable
                .Skip(skip)
                .Take(take ?? 50)
                .ToListAsync();

            return (results.Cast<object>(), totalCount);
        }
    }

    /// <summary>
    /// Handler for players by round performance queries
    /// </summary>
    public class PlayersByRoundPerformanceQueryHandler : IQueryHandler<PlayersByRoundPerformanceQuery>
    {
        public async Task<(IEnumerable<object> Results, int TotalCount)> HandleAsync(
            PlayersByRoundPerformanceQuery query, 
            Type documentType, 
            string? orderBy, 
            int skip, 
            int? take, 
            IDocumentSession session)
        {
            var queryable = session.Query<SuperCoachPlayerDataContract>()
                .Where(x => x.Round == query.Round);

            if (query.MinPoints.HasValue)
            {
                queryable = queryable.Where(x => x.LastRoundScore >= query.MinPoints.Value);
            }

            if (query.MaxPoints.HasValue)
            {
                queryable = queryable.Where(x => x.LastRoundScore <= query.MaxPoints.Value);
            }

            var totalCount = await queryable.CountAsync();

            var orderedQueryable = queryable.ApplyOrdering(orderBy);
            var results = await orderedQueryable
                .Skip(skip)
                .Take(take ?? 50)
                .ToListAsync();

            return (results.Cast<object>(), totalCount);
        }
    }

    /// <summary>
    /// Handler for player text search queries
    /// </summary>
    public class PlayerTextSearchQueryHandler : IQueryHandler<PlayerTextSearchQuery>
    {
        public async Task<(IEnumerable<object> Results, int TotalCount)> HandleAsync(
            PlayerTextSearchQuery query, 
            Type documentType, 
            string? orderBy, 
            int skip, 
            int? take, 
            IDocumentSession session)
        {
            var searchTerm = query.SearchTerm?.Trim();
            if (string.IsNullOrEmpty(searchTerm))
            {
                return (Enumerable.Empty<object>(), 0);
            }

            var queryable = session.Query<SuperCoachPlayerDataContract>()
                .Where(x => (x.FirstName != null && x.FirstName.Contains(searchTerm)) ||
                           (x.LastName != null && x.LastName.Contains(searchTerm)) ||
                           (x.FullName != null && x.FullName.Contains(searchTerm)));

            var totalCount = await queryable.CountAsync();

            var orderedQueryable = queryable.ApplyOrdering(orderBy);
            var results = await orderedQueryable
                .Skip(skip)
                .Take(take ?? 50)
                .ToListAsync();

            return (results.Cast<object>(), totalCount);
        }
    }

    /// <summary>
    /// Extension methods for query building
    /// </summary>
    public static class QueryExtensions
    {
        public static IQueryable<SuperCoachPlayerDataContract> ApplyOrdering(this IQueryable<SuperCoachPlayerDataContract> queryable, string? orderBy)
        {
            if (string.IsNullOrWhiteSpace(orderBy))
            {
                return queryable;
            }

            var orderClauses = orderBy.Split(',', StringSplitOptions.RemoveEmptyEntries);
            IOrderedQueryable<SuperCoachPlayerDataContract>? orderedQuery = null;
            
            foreach (var clause in orderClauses)
            {
                var parts = clause.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var propertyName = parts[0];
                var isDescending = parts.Length > 1 && parts[1].Equals("DESC", StringComparison.OrdinalIgnoreCase);
                
                // Simple property-based ordering - can be extended for more complex scenarios
                if (orderedQuery == null)
                {
                    orderedQuery = propertyName.ToLowerInvariant() switch
                    {
                        "firstname" => isDescending ? queryable.OrderByDescending(x => x.FirstName) : queryable.OrderBy(x => x.FirstName),
                        "lastname" => isDescending ? queryable.OrderByDescending(x => x.LastName) : queryable.OrderBy(x => x.LastName),
                        "currentprice" => isDescending ? queryable.OrderByDescending(x => x.CurrentPrice) : queryable.OrderBy(x => x.CurrentPrice),
                        "totalpoints" => isDescending ? queryable.OrderByDescending(x => x.TotalPoints) : queryable.OrderBy(x => x.TotalPoints),
                        "averagepoints" => isDescending ? queryable.OrderByDescending(x => x.AveragePoints) : queryable.OrderBy(x => x.AveragePoints),
                        "position" => isDescending ? queryable.OrderByDescending(x => x.Position) : queryable.OrderBy(x => x.Position),
                        _ => queryable.OrderBy(x => x.PlayerId)
                    };
                }
                else
                {
                    orderedQuery = propertyName.ToLowerInvariant() switch
                    {
                        "firstname" => isDescending ? orderedQuery.ThenByDescending(x => x.FirstName) : orderedQuery.ThenBy(x => x.FirstName),
                        "lastname" => isDescending ? orderedQuery.ThenByDescending(x => x.LastName) : orderedQuery.ThenBy(x => x.LastName),
                        "currentprice" => isDescending ? orderedQuery.ThenByDescending(x => x.CurrentPrice) : orderedQuery.ThenBy(x => x.CurrentPrice),
                        "totalpoints" => isDescending ? orderedQuery.ThenByDescending(x => x.TotalPoints) : orderedQuery.ThenBy(x => x.TotalPoints),
                        "averagepoints" => isDescending ? orderedQuery.ThenByDescending(x => x.AveragePoints) : orderedQuery.ThenBy(x => x.AveragePoints),
                        "position" => isDescending ? orderedQuery.ThenByDescending(x => x.Position) : orderedQuery.ThenBy(x => x.Position),
                        _ => orderedQuery.ThenBy(x => x.PlayerId)
                    };
                }
            }

            return orderedQuery ?? queryable;
        }
    }

    /// <summary>
    /// SuperCoach Player Data Contract
    /// </summary>
    public class SuperCoachPlayerDataContract
    {
        public int? LastRoundScore { get; set; }
        public decimal? BreakEven { get; set; }
        public int? TotalPoints { get; set; }
        public decimal? AveragePoints { get; set; }
        public decimal? ThreeRoundAveragePoints { get; set; }
        public decimal? FiveRoundAveragePoints { get; set; }
        public decimal? AverageMinutes { get; set; }
        public decimal? ThreeRoundAverageMinutes { get; set; }
        public decimal? FiveRoundAverageMinutes { get; set; }
        public int? TotalMinutesPlayed { get; set; }
        public int? TotalGames { get; set; }
        public int PlayerId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FullName { get; set; }
        public string? Position { get; set; }
        public int? CurrentPrice { get; set; }
        public int? TeamId { get; set; }
        public string? TeamShortName { get; set; }
        public int? JerseyNumber { get; set; }
        public bool? IsOnField { get; set; }
        public string? InjuryStatus { get; set; }
        public decimal? OwnedPercentage { get; set; }
        public decimal? CaptainPercentage { get; set; }
        public bool? IsSuspended { get; set; }
        public int? Season { get; set; }
        public int? Round { get; set; }
        public object? Metadata { get; set; }
    }

    /// <summary>
    /// Request contract for ReadModel endpoint
    /// </summary>
    public sealed class ReadModelQueryContract
    {
        /// <summary>
        /// Gets or sets the unique id of the read model record to get
        /// </summary>
        public Guid? Id { get; set; }
        
        /// <summary>
        /// Gets or sets the search query to perform to find the read model
        /// </summary>
        public ISearchQuery? Query { get; set; }
        
        /// <summary>
        /// Gets or sets one or more property names to order by, comma separated with an optional ASC / DESC modifier for each property name.
        /// </summary>
        public string? OrderBy { get; set; }
        
        /// <summary>
        /// Gets or sets the number of records to skip
        /// </summary>
        public int Skip { get; set; }
        
        /// <summary>
        /// Gets or sets the number of records to take
        /// </summary>
        public int? Take { get; set; }
        
        /// <summary>
        /// Gets or sets the data contract namespace and name e.g. urn:User/UserItem aka Table 
        /// </summary>
        public required string DataType { get; set; }
    }

    /// <summary>
    /// Response contract for ReadModel endpoint
    /// </summary>
    public sealed class ReadModelResponse
    {
        public required object Data { get; set; }
        public int TotalCount { get; set; }
        public int Skip { get; set; }
        public int? Take { get; set; }
        public required string DataType { get; set; }
    }

    /// <summary>
    /// Service for resolving document types from data type strings
    /// </summary>
    public interface IDocumentTypeResolver
    {
        Type ResolveDocumentType(string dataType);
    }

    public class DocumentTypeResolver : IDocumentTypeResolver
    {
        private readonly Dictionary<string, Type> _typeMap;

        public DocumentTypeResolver()
        {
            _typeMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                { "urn:SuperCoach/Player", typeof(SuperCoachPlayerDataContract) },
                { "SuperCoachPlayer", typeof(SuperCoachPlayerDataContract) },
                { "Player", typeof(SuperCoachPlayerDataContract) },
                // Add more mappings as your domain grows
            };
        }

        public Type ResolveDocumentType(string dataType)
        {
            if (string.IsNullOrWhiteSpace(dataType))
            {
                throw new ArgumentException("DataType is required", nameof(dataType));
            }

            if (_typeMap.TryGetValue(dataType, out var type))
            {
                return type;
            }

            throw new ArgumentException($"Unknown data type: {dataType}. Available types: {string.Join(", ", _typeMap.Keys)}", nameof(dataType));
        }
    }

    /// <summary>
    /// Service for handling queries with well-known query types
    /// </summary>
    public interface IQueryService
    {
        Task<(IEnumerable<object> Results, int TotalCount)> ExecuteQueryAsync(
            ISearchQuery query, 
            Type documentType, 
            string? orderBy, 
            int skip, 
            int? take, 
            IDocumentSession session);
    }

    /// <summary>
    /// Enhanced QueryService with dependency injection support
    /// </summary>
    public class QueryService : IQueryService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<QueryService> _logger;

        public QueryService(IServiceProvider serviceProvider, ILogger<QueryService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<(IEnumerable<object> Results, int TotalCount)> ExecuteQueryAsync(
            ISearchQuery query, 
            Type documentType, 
            string? orderBy, 
            int skip, 
            int? take, 
            IDocumentSession session)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var queryType = query.GetType();
            var handlerInterfaceType = typeof(IQueryHandler<>).MakeGenericType(queryType);

            try
            {
                var handler = _serviceProvider.GetService(handlerInterfaceType);
                if (handler == null)
                {
                    throw new InvalidOperationException($"No handler registered for query type: {queryType.Name}");
                }

                // Use reflection to call HandleAsync
                var handleMethod = handlerInterfaceType.GetMethod("HandleAsync");
                if (handleMethod == null)
                {
                    throw new InvalidOperationException($"HandleAsync method not found on handler for query type: {queryType.Name}");
                }

                var task = (Task<(IEnumerable<object>, int)>)handleMethod.Invoke(handler, new object[] 
                { 
                    query, documentType, orderBy, skip, take, session 
                })!;

                var result = await task;
                
                _logger.LogInformation("Executed query {QueryType} returning {Count} results", 
                    queryType.Name, result.Item2);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing query {QueryType}", queryType.Name);
                throw;
            }
        }
    }

    /// <summary>
    /// Registry for mapping query type names to actual types
    /// </summary>
    public interface IQueryTypeRegistry
    {
        Type GetQueryType(string queryTypeName);
        IEnumerable<Type> GetAllQueryTypes();
    }

    public class QueryTypeRegistry : IQueryTypeRegistry
    {
        private readonly Dictionary<string, Type> _queryTypes;

        public QueryTypeRegistry(IEnumerable<Type> queryTypes)
        {
            _queryTypes = queryTypes.ToDictionary(
                type => type.Name.Replace("Query", ""), // Remove "Query" suffix for cleaner names
                type => type,
                StringComparer.OrdinalIgnoreCase);

            // Also add full type names
            foreach (var type in queryTypes)
            {
                _queryTypes.TryAdd(type.Name, type);
            }
        }

        public Type GetQueryType(string queryTypeName)
        {
            if (_queryTypes.TryGetValue(queryTypeName, out var type))
            {
                return type;
            }

            throw new ArgumentException($"Unknown query type: {queryTypeName}");
        }

        public IEnumerable<Type> GetAllQueryTypes()
        {
            return _queryTypes.Values.Distinct();
        }
    }

    /// <summary>
    /// Factory for creating search query instances from JSON
    /// </summary>
    public interface ISearchQueryFactory
    {
        ISearchQuery CreateQuery(string queryType, JsonElement queryData);
    }

    public class SearchQueryFactory : ISearchQueryFactory
    {
        private readonly IQueryTypeRegistry _queryTypeRegistry;

        public SearchQueryFactory(IQueryTypeRegistry queryTypeRegistry)
        {
            _queryTypeRegistry = queryTypeRegistry;
        }

        public ISearchQuery CreateQuery(string queryType, JsonElement queryData)
        {
            var type = _queryTypeRegistry.GetQueryType(queryType);
            var query = JsonSerializer.Deserialize(queryData.GetRawText(), type);
            
            if (query is not ISearchQuery searchQuery)
            {
                throw new InvalidOperationException($"Type {type.Name} does not implement ISearchQuery");
            }

            return searchQuery;
        }
    }

    /// <summary>
    /// Custom JSON converter for ISearchQuery interface that uses the type registry
    /// </summary>
    public class SearchQueryJsonConverter : JsonConverter<ISearchQuery>
    {
        private readonly IQueryTypeRegistry _queryTypeRegistry;

        public SearchQueryJsonConverter(IQueryTypeRegistry queryTypeRegistry)
        {
            _queryTypeRegistry = queryTypeRegistry;
        }

        public override ISearchQuery Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            if (!root.TryGetProperty("queryType", out var queryTypeElement))
            {
                throw new JsonException("Missing 'queryType' property in search query");
            }

            var queryTypeName = queryTypeElement.GetString();
            if (string.IsNullOrEmpty(queryTypeName))
            {
                throw new JsonException("'queryType' property cannot be null or empty");
            }

            try
            {
                var queryType = _queryTypeRegistry.GetQueryType(queryTypeName);
                var query = JsonSerializer.Deserialize(root.GetRawText(), queryType, options);
                
                if (query is not ISearchQuery searchQuery)
                {
                    throw new JsonException($"Type {queryType.Name} does not implement ISearchQuery");
                }

                return searchQuery;
            }
            catch (ArgumentException ex)
            {
                throw new JsonException($"Unknown query type: {queryTypeName}", ex);
            }
        }

        public override void Write(Utf8JsonWriter writer, ISearchQuery value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }

    /// <summary>
    /// Custom model binder for complex query objects
    /// </summary>
    public class SearchQueryModelBinder : IModelBinder
    {
        private readonly ISearchQueryFactory _searchQueryFactory;

        public SearchQueryModelBinder(ISearchQueryFactory searchQueryFactory)
        {
            _searchQueryFactory = searchQueryFactory;
        }

        public bool CanBind(Type type)
        {
            return typeof(ISearchQuery).IsAssignableFrom(type);
        }

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (!CanBind(bindingContext.ModelType))
            {
                return;
            }

            var request = bindingContext.HttpContext.Request;
            
            // Try to read from query string first (for GET requests)
            var queryTypeValue = bindingContext.ValueProvider.GetValue("query.queryType");
            if (queryTypeValue != ValueProviderResult.None)
            {
                var queryType = queryTypeValue.FirstValue;
                if (!string.IsNullOrEmpty(queryType))
                {
                    try
                    {
                        // Build JSON from query string parameters
                        var queryJson = BuildJsonFromQueryString(bindingContext, queryType);
                        var query = _searchQueryFactory.CreateQuery(queryType, queryJson);
                        bindingContext.Result = ModelBindingResult.Success(query);
                        return;
                    }
                    catch (Exception ex)
                    {
                        bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, ex.Message);
                        return;
                    }
                }
            }

            // Try to read from request body (for POST requests)
            if (request.ContentType?.Contains("application/json") == true)
            {
                try
                {
                    using var reader = new StreamReader(request.Body);
                    var json = await reader.ReadToEndAsync();
                    
                    if (!string.IsNullOrEmpty(json))
                    {
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;
                        
                        if (root.TryGetProperty("queryType", out var queryTypeElement))
                        {
                            var queryType = queryTypeElement.GetString();
                            if (!string.IsNullOrEmpty(queryType))
                            {
                                var query = _searchQueryFactory.CreateQuery(queryType, root);
                                bindingContext.Result = ModelBindingResult.Success(query);
                                return;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, ex.Message);
                    return;
                }
            }
        }

        private JsonElement BuildJsonFromQueryString(ModelBindingContext context, string queryType)
        {
            var jsonObject = new Dictionary<string, object> { ["queryType"] = queryType };
            
            // Extract query parameters that start with "query."
            var queryValues = context.ValueProvider.GetValue("query");
            if (queryValues != ValueProviderResult.None)
            {
                // Simple implementation - can be extended for more complex query parameter parsing
                foreach (var value in queryValues.Values!)
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        // Try to parse as number, boolean, or keep as string
                        if (int.TryParse(value, out var intValue))
                            jsonObject["value"] = intValue;
                        else if (bool.TryParse(value, out var boolValue))
                            jsonObject["value"] = boolValue;
                        else if (decimal.TryParse(value, out var decimalValue))
                            jsonObject["value"] = decimalValue;
                        else
                            jsonObject["value"] = value;
                    }
                }
            }

            var json = JsonSerializer.Serialize(jsonObject);
            return JsonDocument.Parse(json).RootElement;
        }
    }

    /// <summary>
    /// Model binder provider for search queries
    /// </summary>
    public class SearchQueryModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (typeof(ISearchQuery).IsAssignableFrom(context.Metadata.ModelType))
            {
                var searchQueryFactory = context.Services.GetRequiredService<ISearchQueryFactory>();
                return new SearchQueryModelBinder(searchQueryFactory);
            }

            return null;
        }
    }

    /// <summary>
    /// FastEndpoints controller for ReadModel queries
    /// </summary>
    public class ReadModelEndpoint : Endpoint<ReadModelQueryContract, ReadModelResponse>
    {
        private readonly IDocumentSession _session;
        private readonly IDocumentTypeResolver _typeResolver;
        private readonly IQueryService _queryService;
        private readonly ILogger<ReadModelEndpoint> _logger;

        public ReadModelEndpoint(
            IDocumentSession session, 
            IDocumentTypeResolver typeResolver, 
            IQueryService queryService,
            ILogger<ReadModelEndpoint> logger)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _typeResolver = typeResolver ?? throw new ArgumentNullException(nameof(typeResolver));
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override void Configure()
        {
            Post("/api/readmodel");
            AllowAnonymous(); // Configure authentication as needed
            Description(d => d
                .WithTags("ReadModel")
                .WithSummary("Query read model documents using well-known queries")
                .WithDescription("Executes queries against Marten document store using predefined query types")
                .Accepts<ReadModelQueryContract>("application/json")
                .Produces<ReadModelResponse>(200, "application/json")
                .ProducesProblem(400)
                .ProducesProblem(500));
        }

        public override async Task HandleAsync(ReadModelQueryContract req, CancellationToken ct)
        {
            _logger.LogInformation("Processing ReadModel request for DataType: {DataType}", req.DataType);

            try
            {
                var documentType = _typeResolver.ResolveDocumentType(req.DataType);

                // Handle single document retrieval by ID
                if (req.Id.HasValue)
                {
                    _logger.LogDebug("Loading document by ID: {Id}", req.Id.Value);
                    
                    var document = await _session.LoadAsync<SuperCoachPlayerDataContract>(req.Id.Value, ct);
                    
                    await SendOkAsync(new ReadModelResponse
                    {
                        Data = document != null ? new[] { document } : Array.Empty<object>(),
                        TotalCount = document != null ? 1 : 0,
                        Skip = 0,
                        Take = 1,
                        DataType = req.DataType
                    }, ct);
                    return;
                }

                // Handle query-based retrieval
                if (req.Query == null)
                {
                    _logger.LogWarning("No query provided and no ID specified");
                    
                    await SendAsync(new ReadModelResponse
                    {
                        Data = Array.Empty<object>(),
                        TotalCount = 0,
                        Skip = req.Skip,
                        Take = req.Take,
                        DataType = req.DataType
                    }, 400, ct);
                    return;
                }

                _logger.LogDebug("Executing query: {QueryType}", req.Query.GetType().Name);

                var (results, totalCount) = await _queryService.ExecuteQueryAsync(
                    req.Query, 
                    documentType, 
                    req.OrderBy, 
                    req.Skip, 
                    req.Take, 
                    _session);

                await SendOkAsync(new ReadModelResponse
                {
                    Data = results,
                    TotalCount = totalCount,
                    Skip = req.Skip,
                    Take = req.Take,
                    DataType = req.DataType
                }, ct);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request argument");
                await SendAsync(new ReadModelResponse
                {
                    Data = new { Error = ex.Message },
                    TotalCount = 0,
                    Skip = req.Skip,
                    Take = req.Take,
                    DataType = req.DataType
                }, 400, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing ReadModel request");
                await SendAsync(new ReadModelResponse
                {
                    Data = new { Error = "An error occurred while processing the request" },
                    TotalCount = 0,
                    Skip = req.Skip,
                    Take = req.Take,
                    DataType = req.DataType
                }, 500, ct);
            }
        }
    }

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

            // Configure Marten
            builder.Services.AddMarten(options =>
            {
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                    ?? "[REDACTED]";
                
                options.Connection(connectionString);
                
                // Register document types
                options.RegisterDocumentType<SuperCoachPlayerDataContract>();
                
                // Configure document storage
                options.Schema.For<SuperCoachPlayerDataContract>()
                    .Identity(x => x.PlayerId)
                    .Index(x => x.TeamId)
                    .Index(x => x.Position)
                    .Index(x => x.Season)
                    .Index(x => x.Round);

                // Schema creation can be handled separately
                // For development, you may need to run migrations manually
            });

            // Register FastEndpoints
            builder.Services.AddFastEndpoints();

            builder.Services.AddSingleton<IQueryService, QueryService>();

            // Auto-register all query handlers using reflection
            RegisterQueryHandlers(builder.Services);

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
            app.UseFastEndpoints(c =>
            {
                c.Serializer.Options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                c.Serializer.Options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                
                // Add our custom converter
                var queryTypeRegistry = app.Services.GetRequiredService<IQueryTypeRegistry>();
                var searchQueryConverter = new SearchQueryJsonConverter(queryTypeRegistry);
                c.Serializer.Options.Converters.Add(searchQueryConverter);
            });

            // Seed sample data in development
            if (app.Environment.IsDevelopment())
            {
                SeedSampleData(app.Services);
            }

            app.Run();
        }

        /// <summary>
        /// Automatically registers all query handlers using reflection
        /// </summary>
        private static void RegisterQueryHandlers(IServiceCollection services)
        {
            var assemblies = new[] { Assembly.GetExecutingAssembly() };
            
            foreach (var assembly in assemblies)
            {
                // Find all types that implement IQueryHandler<T>
                var handlerTypes = assembly.GetTypes()
                    .Where(type => !type.IsAbstract && !type.IsInterface)
                    .Where(type => type.GetInterfaces()
                        .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<>)))
                    .ToList();

                foreach (var handlerType in handlerTypes)
                {
                    var handlerInterfaces = handlerType.GetInterfaces()
                        .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<>));

                    foreach (var handlerInterface in handlerInterfaces)
                    {
                        services.AddScoped(handlerInterface, handlerType);
                        
                        // Also register as the concrete type for the QueryService
                        services.AddScoped(handlerType);
                    }
                }
            }
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
}