using ReadModelQuery.Core.Interfaces;

namespace ReadModelQuery.Core;

/// <summary>
/// Contract for read model query requests
/// </summary>
public sealed class ReadModelQueryContract
{
    /// <summary>
    /// Optional unique identifier for the request
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// The query object containing query-specific parameters
    /// </summary>
    public ISearchQuery? Query { get; set; }

    /// <summary>
    /// Optional ordering specification (e.g., "lastName ASC, firstName DESC")
    /// </summary>
    public string? OrderBy { get; set; }

    /// <summary>
    /// Number of records to skip (for pagination)
    /// </summary>
    public int Skip { get; set; }

    /// <summary>
    /// Number of records to take (for pagination). If null, returns all remaining records.
    /// </summary>
    public int? Take { get; set; }

    /// <summary>
    /// The type of document/data being queried (e.g., "SuperCoachPlayer")
    /// </summary>
    public required string DataType { get; set; }
} 