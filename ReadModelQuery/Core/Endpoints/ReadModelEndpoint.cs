using FastEndpoints;
using ReadModelQuery.Core;
using ReadModelQuery.Core.Services;
using Marten;

namespace ReadModelQuery.Core.Endpoints;

/// <summary>
/// FastEndpoints endpoint for read model queries
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
        try
        {
            _logger.LogInformation("Processing read model query: {QueryType}", req.Query?.QueryType ?? "Unknown");

            if (req.Query == null)
            {
                await SendErrorsAsync(400, ct);
                return;
            }

            // Resolve the document type
            var documentType = _typeResolver.ResolveDocumentType(req.DataType);
            
            _logger.LogDebug("Resolved document type: {DocumentType}", documentType.Name);

            // Execute the query
            var (results, totalCount) = await _queryService.ExecuteQueryAsync(
                req.Query, 
                documentType, 
                req.OrderBy, 
                req.Skip, 
                req.Take, 
                _session);

            var response = new ReadModelResponse
            {
                Data = results,
                TotalCount = totalCount,
                Skip = req.Skip,
                Take = req.Take,
                DataType = req.DataType
            };

            _logger.LogInformation("Query executed successfully. Returned {Count} of {Total} records", 
                results.Count(), totalCount);

            await SendOkAsync(response, ct);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in query request");
            await SendErrorsAsync(400, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing read model query");
            await SendErrorsAsync(500, ct);
        }
    }
} 