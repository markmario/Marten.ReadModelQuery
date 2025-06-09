namespace ReadModelQuery.Core;

/// <summary>
/// Response contract for read model queries
/// </summary>
public sealed class ReadModelResponse
{
    public required object Data { get; set; }
    public int TotalCount { get; set; }
    public int Skip { get; set; }
    public int? Take { get; set; }
    public required string DataType { get; set; }
} 