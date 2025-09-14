namespace Core.Models;

public class SystemHealthStatus
{
  public double EncryptionPerformance { get; set; } = 100.0;
  public double AuditLogPerformance { get; set; } = 100.0;
  public double VitalsProcessingRate { get; set; } = 1.0; // Updates per second
  public int MemoryUsageMb { get; set; }
  public TimeSpan Uptime { get; set; }
}