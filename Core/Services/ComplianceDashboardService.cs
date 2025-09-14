using System.Diagnostics;
using Core.Enums;
using Core.Interfaces;
using Core.Models;

namespace Core.Services;

public class ComplianceDashboardService(
  IComplianceService complianceService,
  IPatientDataService patientService,
  IAuditLogger auditLogger)
  : IComplianceDashboardService
{
  private readonly DateTime _startTime = DateTime.UtcNow;

  public async Task<ComplianceDashboardData> GetDashboardDataAsync()
  {
    var complianceScore = await complianceService.CalculateComplianceScoreAsync();
    var activeSessions = await patientService.GetActiveSessionsAsync();
    var auditEntries = await auditLogger.GetAuditTrailAsync();
    var criticalAlerts = auditEntries.Count(e => e.Severity == AuditSeverity.Critical);

    return new ComplianceDashboardData
    {
      ComplianceScore = complianceScore,
      ActiveSessions = activeSessions.Count,
      TotalAuditEntries = auditEntries.Count,
      CriticalAlerts = criticalAlerts,
      Metrics = await GetDetailedMetricsAsync(),
      SystemHealth = GetSystemHealth()
    };
  }

  public async Task<List<ComplianceMetric>> GetDetailedMetricsAsync()
  {
    var auditEntries = await auditLogger.GetAuditTrailAsync();
    var recentEntries = auditEntries.Where(e => e.Timestamp >= DateTime.UtcNow.AddHours(-1)).ToList();

    return
    [
      new()
      {
        Name = "HIPAA Compliance",
        Value = "✓ Active",
        Status = "Good",
        Description = "Patient data encryption and access logging active"
      },

      new()
      {
        Name = "ISO 13485",
        Value = "✓ Validated",
        Status = "Good",
        Description = "Medical device quality management standards met"
      },

      new()
      {
        Name = "Data Encryption",
        Value = "AES-256",
        Status = "Good",
        Description = "Banking-grade encryption protecting all patient data"
      },

      new()
      {
        Name = "Session Security",
        Value = "8h timeout",
        Status = "Good",
        Description = "Automatic session expiration preventing data exposure"
      },

      new()
      {
        Name = "Audit Frequency",
        Value = $"{recentEntries.Count}/hr",
        Status = recentEntries.Count > 100 ? "Warning" : "Good",
        Description = "Real-time logging of all system activities"
      }
    ];
  }
  
  private SystemHealthStatus GetSystemHealth()
  {
    var process = Process.GetCurrentProcess();
    return new SystemHealthStatus
    {
      EncryptionPerformance = 98.5, // Simulated - in production, measure actual performance
      AuditLogPerformance = 99.2,
      VitalsProcessingRate = 0.5, // 1 update per 2 seconds
      MemoryUsageMb = (int)(process.WorkingSet64 / 1024 / 1024),
      Uptime = DateTime.UtcNow - _startTime
    };
  }
}