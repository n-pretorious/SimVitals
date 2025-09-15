namespace Core.Models;

public class ComponentHealthStatus
{
  public string ComponentName { get; set; } = string.Empty;
  public bool IsHealthy { get; set; } = true;
  public string Status { get; set; } = "OK";
  public double ResponseTimeMs { get; set; }
  public DateTime LastChecked { get; set; } = DateTime.UtcNow;
  public Dictionary<string, object> Metrics { get; set; } = new();
}