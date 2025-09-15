namespace Core.Models;

public class PerformanceReport
{
  public string OperationName { get; set; } = string.Empty;
  public int TotalOperations { get; set; }
  public double AverageResponseTimeMs { get; set; }
  public double MedianResponseTimeMs { get; set; }
  public double Percentile95Ms { get; set; }
  public double Percentile99Ms { get; set; }
  public double SuccessRate { get; set; }
  public double ThroughputOperationsPerSecond { get; set; }
  public TimeSpan TimeWindow { get; set; }
  public int SloViolations { get; set; }
}