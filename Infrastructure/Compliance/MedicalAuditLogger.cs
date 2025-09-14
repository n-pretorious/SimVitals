using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Core.Enums;
using Core.Interfaces;
using Core.Models;

namespace Infrastructure.Compliance;

public class MedicalAuditLogger : IAuditLogger
{
  private readonly IEncryptionService _encryptionService;
  private readonly List<AuditEntry> _auditEntries = new();
  
  public MedicalAuditLogger(IEncryptionService encryptionService)
  {
    _encryptionService = encryptionService;
  }
  
  public async Task LogAsync(AuditEntry entry)
  {
    var entryJson = JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = false });
    entry.IntegrityHash = _encryptionService.ComputeHash(entryJson);

    _auditEntries.Add(entry);

    // In production: store in tamper-evident database
    await Task.CompletedTask;

    // Real-time compliance monitoring (like banking fraud detection)
    await MonitorComplianceViolations(entry);
  }

  public Task<List<AuditEntry>> GetAuditTrailAsync(string? patientToken = null, DateTime? from = null, DateTime? to = null)
  {
    var query = _auditEntries.AsQueryable();

    if (!string.IsNullOrEmpty(patientToken))
      query = query.Where(e => e.PatientToken == patientToken);

    if (from.HasValue)
      query = query.Where(e => e.Timestamp >= from.Value);

    if (to.HasValue)
      query = query.Where(e => e.Timestamp <= to.Value);

    return Task.FromResult(query.OrderByDescending(e => e.Timestamp).ToList());
  }

  public async Task<ComplianceReport> GenerateComplianceReportAsync(DateTime from, DateTime to)
  {
    var entries = await GetAuditTrailAsync(null, from, to);
    var violations = entries.Where(e => e.Severity >= AuditSeverity.Warning).ToList();

    var report = new ComplianceReport
    {
      PeriodFrom = from,
      PeriodTo = to,
      TotalEntries = entries.Count,
      ComplianceViolations = violations.Count,
      ComplianceScore = entries.Count > 0 ? (1.0 - (double)violations.Count / entries.Count) * 100 : 100,
      Violations = violations.Select(v => $"{v.Timestamp:yyyy-MM-dd HH:mm:ss} - {v.Action}: {v.Details}").ToList()
    };

    return report;
  }
  
  private async Task MonitorComplianceViolations(AuditEntry entry)
  {
    // Pattern detection for compliance violations (like banking fraud detection)
    var recentEntries = _auditEntries
      .Where(e => e.UserId == entry.UserId)
      .Where(e => e.Timestamp >= DateTime.UtcNow.AddMinutes(-10))
      .ToList();

    // Detect suspicious patterns
    if (recentEntries.Count(e => e.Action.Contains("ACCESS")) > 50)
    {
      await LogAsync(new AuditEntry
      {
        Action = "COMPLIANCE_VIOLATION_DETECTED",
        UserId = entry.UserId,
        Details = "Excessive access detected - potential data mining",
        Severity = AuditSeverity.Critical,
        ComplianceFlags = ComplianceFlags.HIPAA
      });
    }
  }
}