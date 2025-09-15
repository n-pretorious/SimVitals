namespace Core.Models;

public class SystemHealthMetrics
{
  public double CpuUsagePercent { get; set; }
  public long MemoryUsageBytes { get; set; }
  public int ActiveConnections { get; set; }
  public double ResponseTimeMs { get; set; }
  public int ErrorRate { get; set; }
  public DateTime LastHealthCheck { get; set; } = DateTime.UtcNow;
  public List<string> HealthWarnings { get; set; } = new();
  public string SystemStatus { get; set; } = "Operational";
  public TimeSpan Uptime { get; set; }
  public double ThroughputOperationsPerSecond { get; set; }
}