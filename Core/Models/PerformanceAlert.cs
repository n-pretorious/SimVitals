using Core.Enums;

namespace Core.Models;

public class PerformanceAlert
{
  public AlertSeverity Severity { get; set; }
  public string Message { get; set; } = string.Empty;
  public string Metric { get; set; } = string.Empty;
  public double Value { get; set; }
  public double Threshold { get; set; }
  public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}