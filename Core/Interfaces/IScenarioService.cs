using Core.Models;

namespace Core.Interfaces;

public interface IScenarioService
{
  List<MedicalScenario> GetAvailableScenarios();
  Task<bool> StartScenarioAsync(MedicalScenario scenario, string instructorId);
  Task<MedicalScenario?> GetCurrentScenarioAsync();
  Task LogScenarioProgressAsync(ScenarioStep step, string instructorId);
}