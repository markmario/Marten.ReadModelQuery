namespace ReadModelQuery.Features.SuperCoachPlayer.Models;

/// <summary>
/// Data contract for SuperCoach player information
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