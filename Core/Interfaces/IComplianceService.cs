using Core.Models;

namespace Core.Interfaces;

public interface IComplianceService
{
  Task<bool> ValidateVitalsSafetyAsync(VitalSigns vitals, string patientToken);
  Task<List<ComplianceViolation>> DetectViolationsAsync(string userId, TimeSpan timeWindow);
  Task<double> CalculateComplianceScoreAsync();
  Task TriggerSafetyAlertAsync(SafetyAlert alert);
}