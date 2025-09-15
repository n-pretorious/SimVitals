namespace Core.Models;

public class PerformanceMetric
{
  public string OperationName { get; set; } = string.Empty;
  public TimeSpan Duration { get; set; }
  public DateTime Timestamp { get; set; }
  public bool IsSuccess { get; set; }
  public string? AdditionalInfo { get; set; }
}