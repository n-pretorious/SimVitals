using Core.Enums;

namespace Core.Models;

public class MedicalScenario
{
  public Guid Id { get; set; } = Guid.NewGuid();
  public string Name { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public ScenarioCategory Category { get; set; }
  public ScenarioDifficulty Difficulty { get; set; }
  public VitalSigns InitialVitals { get; set; } = new();
  public VitalSigns TargetVitals { get; set; } = new();
  public List<ScenarioStep> Steps { get; set; } = new();
  public TimeSpan ExpectedDuration { get; set; }
  public List<string> LearningObjectives { get; set; } = new();
  public bool RequiresSupervisorApproval { get; set; }
}