using Core.Enums;

namespace Core.Models;

public class SystemAlert
{
  public Guid Id { get; set; } = Guid.NewGuid();
  public DateTime Timestamp { get; set; } = DateTime.UtcNow;
  public AlertSeverity Severity { get; set; }
  public string Title { get; set; } = string.Empty;
  public string Message { get; set; } = string.Empty;
  public string Component { get; set; } = string.Empty;
  public Exception? Exception { get; set; }
  public bool IsResolved { get; set; }
  public string? ResolutionAction { get; set; }
  public Dictionary<string, object> AdditionalData { get; set; } = new();
}