using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Core.Interfaces;
using Core.Models;
using ReactiveUI;

namespace SimVitals.ViewModels;

public class CompliancePanelViewModel : ViewModelBase
{
  private readonly IComplianceDashboardService _dashboardService;
  private readonly CompositeDisposable _disposables = new();

  private ComplianceDashboardData _dashboardData = new();
  public ComplianceDashboardData DashboardData
  {
    get => _dashboardData;
    private set => this.RaiseAndSetIfChanged(ref _dashboardData, value);
  }
  
  public ObservableCollection<ComplianceMetric> DetailedMetrics { get; } = new();

  public ReactiveCommand<Unit, Unit> RefreshDataCommand { get; }
  public ReactiveCommand<Unit, Unit> ExportComplianceReportCommand { get; }
  
  public CompliancePanelViewModel(IComplianceDashboardService dashboardService)
  {
    _dashboardService = dashboardService;

    RefreshDataCommand = ReactiveCommand.CreateFromTask(RefreshDashboardData);
    ExportComplianceReportCommand = ReactiveCommand.Create(ExportComplianceReport);

    // Auto-refresh every 10 seconds
    Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(10))
      .Subscribe(_ => RefreshDataCommand.Execute().Subscribe())
      .DisposeWith(_disposables);
  }
  
  private async Task RefreshDashboardData()
  {
    try
    {
      DashboardData = await _dashboardService.GetDashboardDataAsync();
            
      // Update detailed metrics collection
      DetailedMetrics.Clear();
      foreach (var metric in DashboardData.Metrics)
      {
        DetailedMetrics.Add(metric);
      }

      Console.WriteLine($"Dashboard refreshed: {DashboardData.ComplianceScore:F1}% compliance, {DashboardData.ActiveSessions} sessions, {DashboardData.SystemHealth.MemoryUsageMb}MB memory");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Failed to refresh dashboard: {ex.Message}");
    }
  }
  
  private void ExportComplianceReport()
  {
    var uptimeFormatted = DashboardData.SystemHealth.Uptime.ToString(@"dd\.hh\:mm\:ss");
    
    var reportData = $"""
                      === MEDICAL DEVICE COMPLIANCE REPORT ===
                      Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

                      COMPLIANCE SUMMARY:
                      Overall Score: {DashboardData.ComplianceScore:F1}%
                      Active Patient Sessions: {DashboardData.ActiveSessions}
                      Total Audit Entries: {DashboardData.TotalAuditEntries}
                      Critical Alerts: {DashboardData.CriticalAlerts}

                      SYSTEM HEALTH:
                      Uptime: {uptimeFormatted}
                      Memory Usage: {DashboardData.SystemHealth.MemoryUsageMb} MB
                      Encryption Performance: {DashboardData.SystemHealth.EncryptionPerformance:F1}%
                      Audit Log Performance: {DashboardData.SystemHealth.AuditLogPerformance:F1}%

                      REGULATORY COMPLIANCE:
                      """;
        
    Console.WriteLine("=== COMPLIANCE REPORT EXPORT ===");
    Console.WriteLine(reportData);
    Console.WriteLine("=== END REPORT ===");
  }

  public void Dispose()
  {
    _disposables.Dispose();
  }
}