namespace Core.Models;

public class ScenarioStep
{
  public int Order { get; set; }
  public string Description { get; set; } = string.Empty;
  public VitalSigns ExpectedVitals { get; set; } = new();
  public TimeSpan Duration { get; set; }
  public string InstructorNotes { get; set; } = string.Empty;
}