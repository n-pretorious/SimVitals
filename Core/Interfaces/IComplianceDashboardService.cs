using Core.Models;

namespace Core.Interfaces;

public interface IComplianceDashboardService
{
  Task<ComplianceDashboardData> GetDashboardDataAsync();
  Task<List<ComplianceMetric>> GetDetailedMetricsAsync();
}