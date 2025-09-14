namespace Core.Models;

public class ComplianceDashboardData
{
  public double ComplianceScore { get; set; } = 100.0;
  public int ActiveSessions { get; set; }
  public int TotalAuditEntries { get; set; }
  public int CriticalAlerts { get; set; }
  public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
  public List<ComplianceMetric> Metrics { get; set; } = new();
  public SystemHealthStatus SystemHealth { get; set; } = new();
}