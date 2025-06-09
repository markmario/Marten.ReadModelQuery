using ReadModelQuery.Core.Interfaces;
using Marten;

namespace ReadModelQuery.Core.Services;

/// <summary>
/// Service for executing queries using registered handlers
/// </summary>
public class QueryService : IQueryService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QueryService> _logger;

    public QueryService(IServiceProvider serviceProvider, ILogger<QueryService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<(IEnumerable<object> Results, int TotalCount)> ExecuteQueryAsync(
        ISearchQuery query, 
        Type documentType, 
        string? orderBy, 
        int skip, 
        int? take, 
        IDocumentSession session)
    {
        var queryType = query.GetType();
        var handlerInterfaceType = typeof(IQueryHandler<>).MakeGenericType(queryType);
        
        _logger.LogDebug("Looking for handler of type: {HandlerType}", handlerInterfaceType.Name);
        
        var handler = _serviceProvider.GetService(handlerInterfaceType);
        if (handler == null)
        {
            throw new InvalidOperationException($"No handler registered for query type: {queryType.Name}");
        }

        _logger.LogDebug("Found handler: {HandlerName}", handler.GetType().Name);

        // Use reflection to call the HandleAsync method
        var handleMethod = handlerInterfaceType.GetMethod("HandleAsync");
        if (handleMethod == null)
        {
            throw new InvalidOperationException($"HandleAsync method not found on handler for {queryType.Name}");
        }

        var task = (Task<(IEnumerable<object> Results, int TotalCount)>)handleMethod.Invoke(
            handler, 
            new object[] { query, documentType, orderBy, skip, take, session })!;

        return await task;
    }
} 