using Core.Enums;
using Core.Interfaces;
using Core.Models;

namespace Core.Services;

public class ScenarioService(IAuditLogger auditLogger) : IScenarioService
{
  private readonly IAuditLogger _auditLogger = auditLogger;
  private MedicalScenario? _currentScenario;

  public List<MedicalScenario> GetAvailableScenarios()
  {
        return
        [
            new MedicalScenario
            {
                Name = "Cardiac Arrest Protocol",
                Description = "Complete cardiac arrest simulation with CPR protocol",
                Category = ScenarioCategory.Cardiovascular,
                Difficulty = ScenarioDifficulty.Advanced,
                InitialVitals = new VitalSigns
                    { HeartRate = 72, BloodPressure = new() { Systolic = 120, Diastolic = 80 }, OxygenSaturation = 98 },
                TargetVitals = new VitalSigns
                    { HeartRate = 0, BloodPressure = new() { Systolic = 0, Diastolic = 0 }, OxygenSaturation = 75 },
                ExpectedDuration = TimeSpan.FromMinutes(15),
                LearningObjectives = new()
                {
                    "Recognize cardiac arrest signs",
                    "Perform effective CPR",
                    "Use AED properly",
                    "Follow ACLS protocol"
                },
                RequiresSupervisorApproval = true
            },

            new MedicalScenario
            {
                Name = "Anaphylactic Shock",
                Description = "Severe allergic reaction with airway compromise",
                Category = ScenarioCategory.Emergency,
                Difficulty = ScenarioDifficulty.Intermediate,
                InitialVitals = new VitalSigns
                    { HeartRate = 85, BloodPressure = new() { Systolic = 130, Diastolic = 85 }, OxygenSaturation = 96 },
                TargetVitals = new VitalSigns
                    { HeartRate = 150, BloodPressure = new() { Systolic = 80, Diastolic = 40 }, OxygenSaturation = 85 },
                ExpectedDuration = TimeSpan.FromMinutes(10),
                LearningObjectives = new()
                {
                    "Identify anaphylaxis symptoms",
                    "Administer epinephrine",
                    "Manage airway obstruction"
                }
            },

            new MedicalScenario
            {
                Name = "Acute Stroke Assessment",
                Description = "Neurological assessment and time-critical intervention",
                Category = ScenarioCategory.Neurological,
                Difficulty = ScenarioDifficulty.Advanced,
                InitialVitals = new VitalSigns
                    { HeartRate = 78, BloodPressure = new() { Systolic = 140, Diastolic = 90 }, OxygenSaturation = 97 },
                TargetVitals = new VitalSigns
                {
                    HeartRate = 95, BloodPressure = new() { Systolic = 180, Diastolic = 110 }, OxygenSaturation = 92
                },
                ExpectedDuration = TimeSpan.FromMinutes(20),
                LearningObjectives = new()
                {
                    "Perform FAST assessment",
                    "Recognize stroke symptoms",
                    "Activate stroke protocol"
                },
                RequiresSupervisorApproval = true
            }
        ];
  }

  public async Task<bool> StartScenarioAsync(MedicalScenario scenario, string instructorId)
  {
      if (scenario.RequiresSupervisorApproval)
      {
          // In production: check instructor permissions
          await _auditLogger.LogAsync(new AuditEntry
          {
              Action = "ADVANCED_SCENARIO_STARTED",
              UserId = instructorId,
              Details = $"Advanced scenario '{scenario.Name}' requires supervisor approval",
              Severity = AuditSeverity.Warning,
              ComplianceFlags = ComplianceFlags.ISO13485
          });
      }
      
      _currentScenario = scenario;

      await _auditLogger.LogAsync(new AuditEntry
      {
          Action = "MEDICAL_SCENARIO_STARTED",
          UserId = instructorId,
          Details = $"Started {scenario.Category} scenario: {scenario.Name} (Difficulty: {scenario.Difficulty})",
          ComplianceFlags = ComplianceFlags.ISO13485
      });

      return true;
  }

  public Task<MedicalScenario?> GetCurrentScenarioAsync()
  {
      return Task.FromResult(_currentScenario);
  }

  public async Task LogScenarioProgressAsync(ScenarioStep step, string instructorId)
  {
      await _auditLogger.LogAsync(new AuditEntry
      {
          Action = "SCENARIO_STEP_COMPLETED",
          UserId = instructorId,
          Details = $"Completed step {step.Order}: {step.Description}",
          ComplianceFlags = ComplianceFlags.ISO13485
      });
  }
}