using Core.Enums;

namespace Core.Models;

public class ComplianceViolation
{
  public Guid Id { get; set; } = Guid.NewGuid();
  public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
  public string ViolationType { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public string UserId { get; set; } = string.Empty;
  public string PatientToken { get; set; } = string.Empty;
  public ViolationSeverity Severity { get; set; }
  public bool IsResolved { get; set; }
}