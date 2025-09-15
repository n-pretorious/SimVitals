using Core.Models;

namespace Core.Interfaces;

public interface ISystemHealthService
{
  Task<SystemHealthMetrics> GetSystemHealthAsync();
  Task<List<SystemAlert>> GetActiveAlertsAsync();
  Task<List<ComponentHealthStatus>> GetComponentHealthAsync();
  Task TriggerHealthCheckAsync();
  Task<bool> IsSystemHealthyAsync();
  Task ResolveAlertAsync(Guid alertId, string resolutionAction);
  event EventHandler<SystemAlert> AlertGenerated;
}