using Core.Enums;

namespace Core.Models;

public class AuditEntry
{
  public DateTime Timestamp { get; set; } = DateTime.UtcNow;
  public string Action { get; set; } = string.Empty;
  public string UserId { get; set; } = string.Empty;
  public string Details { get; set; } = string.Empty;
  public string PatientToken { get; set; } = string.Empty;
    
  public string TimeString => Timestamp.ToString("HH:mm:ss");
  public AuditSeverity Severity { get; set; }
  public ComplianceFlags ComplianceFlags { get; set; }
  public string IntegrityHash { get; set; }
}