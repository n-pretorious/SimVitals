using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Enums;
using Core.Interfaces;
using Core.Models;

namespace Core.Services;

public class ComplianceService : IComplianceService
{
  private readonly IAuditLogger _auditLogger;
  private readonly List<ComplianceViolation> _violations = new();
  private readonly List<SafetyAlert> _safetyAlerts = new();
  
  public ComplianceService(IAuditLogger auditLogger)
  {
    _auditLogger = auditLogger;
  }


  public async Task<bool> ValidateVitalsSafetyAsync(VitalSigns vitals, string patientToken)
  {
    var violations = new List<string>();
        
    // Critical safety ranges (like banking transaction limits)
    if (vitals.HeartRate < 0 || vitals.HeartRate > 300)
      violations.Add($"Heart rate {vitals.HeartRate} outside safe range (0-300)");
            
    if (vitals.BloodPressure.Systolic < 0 || vitals.BloodPressure.Systolic > 300)
      violations.Add($"Systolic BP {vitals.BloodPressure.Systolic} outside safe range (0-300)");
            
    if (vitals.OxygenSaturation < 0 || vitals.OxygenSaturation > 100)
      violations.Add($"O2 saturation {vitals.OxygenSaturation} outside safe range (0-100)");

    if (violations.Any())
    {
      var alert = new SafetyAlert
      {
        AlertType = "VITALS_SAFETY_VIOLATION",
        Message = string.Join("; ", violations),
        VitalsAtAlert = vitals,
        PatientToken = patientToken
      };
            
      await TriggerSafetyAlertAsync(alert);
      return false;
    }

    return true;
  }

  public async Task<List<ComplianceViolation>> DetectViolationsAsync(string userId, TimeSpan timeWindow)
  {
    var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
    var recentEntries = await _auditLogger.GetAuditTrailAsync(null, cutoffTime, DateTime.UtcNow);
        
    var userEntries = recentEntries.Where(e => e.UserId == userId).ToList();
    var violations = new List<ComplianceViolation>();
    
    // Excessive access pattern
    if (userEntries.Count(e => e.Action.Contains("PATIENT_DATA")) > 50)
    {
      violations.Add(new ComplianceViolation
      {
        ViolationType = "EXCESSIVE_DATA_ACCESS",
        Description = $"User accessed patient data {userEntries.Count} times in {timeWindow}",
        UserId = userId,
        Severity = ViolationSeverity.High
      });
    }
    
    // Rapid session creation pattern
    var sessionCreations = userEntries.Count(e => e.Action == "PATIENT_SESSION_CREATED");
    if (sessionCreations > 10)
    {
      violations.Add(new ComplianceViolation
      {
        ViolationType = "RAPID_SESSION_CREATION",
        Description = $"Created {sessionCreations} patient sessions rapidly",
        UserId = userId,
        Severity = ViolationSeverity.Medium
      });
    }
    
    // Log detected violations
    foreach (var violation in violations)
    {
      await _auditLogger.LogAsync(new AuditEntry
      {
        Action = "COMPLIANCE_VIOLATION_DETECTED",
        UserId = userId,
        Details = $"{violation.ViolationType}: {violation.Description}",
        Severity = AuditSeverity.Critical
      });
    }
    
    _violations.AddRange(violations);
    return violations;
  }

  public async Task<double> CalculateComplianceScoreAsync()
  {
    var totalEntries = await _auditLogger.GetAuditTrailAsync();
    if (!totalEntries.Any()) return 100.0;

    var criticalEntries = totalEntries.Count(e => e.Severity >= AuditSeverity.Error);
    var recentViolations = _violations.Count(v => v.DetectedAt >= DateTime.UtcNow.AddHours(-24));

    // Calculate score (banking-style risk assessment)
    var baseScore = 100.0;
    var errorPenalty = (double)criticalEntries / totalEntries.Count * 30;
    var violationPenalty = Math.Min(recentViolations * 10, 50);

    var score = Math.Max(0, baseScore - errorPenalty - violationPenalty);
    return Math.Round(score, 1);
  }

  public async Task TriggerSafetyAlertAsync(SafetyAlert alert)
  {
    _safetyAlerts.Add(alert);
        
    await _auditLogger.LogAsync(new AuditEntry
    {
      Action = "SAFETY_ALERT_TRIGGERED",
      PatientToken = alert.PatientToken,
      Details = $"{alert.AlertType}: {alert.Message}",
      Severity = AuditSeverity.Critical,
      ComplianceFlags = ComplianceFlags.ISO13485
    });

    // In production: send alerts to monitoring systems, email administrators, etc.
    Console.WriteLine($"SAFETY ALERT: {alert.AlertType} - {alert.Message}");
  }
}