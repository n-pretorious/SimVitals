using Core.Models;

namespace Core.Interfaces;

public interface IAuditLogger
{
  Task LogAsync(AuditEntry entry);
  Task<List<AuditEntry>> GetAuditTrailAsync(string? patientToken = null, DateTime? from = null, DateTime? to = null);
  Task<ComplianceReport> GenerateComplianceReportAsync(DateTime from, DateTime to);
}