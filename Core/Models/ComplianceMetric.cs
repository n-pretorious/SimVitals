namespace Core.Models;

public class ComplianceMetric
{
  public string Name { get; set; } = string.Empty;
  public string Value { get; set; } = string.Empty;
  public string Status { get; set; } = string.Empty; // "Good", "Warning", "Critical"
  public string Description { get; set; } = string.Empty;
}