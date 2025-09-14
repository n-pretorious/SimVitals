using System;

namespace Core.Models;

public class SafetyAlert
{
  public DateTime Timestamp { get; set; } = DateTime.UtcNow;
  public string AlertType { get; set; } = string.Empty;
  public string Message { get; set; } = string.Empty;
  public VitalSigns VitalsAtAlert { get; set; } = new();
  public string PatientToken { get; set; } = string.Empty;
}