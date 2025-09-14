using System;
using System.Collections.Generic;

namespace Core.Models;

public class ComplianceReport
{
  public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
  public DateTime PeriodFrom { get; set; }
  public DateTime PeriodTo { get; set; }
  public int TotalEntries { get; set; }
  public int ComplianceViolations { get; set; }
  public double ComplianceScore { get; set; }
  public List<string> Violations { get; set; } = new();
}