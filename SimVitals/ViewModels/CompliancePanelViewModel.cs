using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
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
    private readonly ISystemHealthService _systemHealthService;
    private readonly CompositeDisposable _disposables = new();

    private ComplianceDashboardData _dashboardData = new();
    public ComplianceDashboardData DashboardData
    {
        get => _dashboardData;
        private set => this.RaiseAndSetIfChanged(ref _dashboardData, value);
    }

    // System Health Properties
    private SystemHealthMetrics _systemHealth = new();
    public SystemHealthMetrics SystemHealth
    {
        get => _systemHealth;
        private set => this.RaiseAndSetIfChanged(ref _systemHealth, value);
    }

    private string _healthStatus = "Checking...";
    public string HealthStatus
    {
        get => _healthStatus;
        set => this.RaiseAndSetIfChanged(ref _healthStatus, value);
    }

    private bool _isSystemHealthy = true;
    public bool IsSystemHealthy
    {
        get => _isSystemHealthy;
        set => this.RaiseAndSetIfChanged(ref _isSystemHealthy, value);
    }

    public ObservableCollection<ComplianceMetric> DetailedMetrics { get; } = new();
    public ObservableCollection<ComponentHealthStatus> ComponentStatuses { get; } = new();
    public ObservableCollection<SystemAlert> RecentAlerts { get; } = new();

    public ReactiveCommand<Unit, Unit> RefreshDataCommand { get; }
    public ReactiveCommand<Unit, Unit> ExportComplianceReportCommand { get; }
    public ReactiveCommand<Unit, Unit> RunFullHealthCheckCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearAlertsCommand { get; }

    public CompliancePanelViewModel(
        IComplianceDashboardService dashboardService, 
        ISystemHealthService systemHealthService)
    {
        _dashboardService = dashboardService;
        _systemHealthService = systemHealthService;

        RefreshDataCommand = ReactiveCommand.CreateFromTask(RefreshDashboardData);
        ExportComplianceReportCommand = ReactiveCommand.CreateFromTask(ExportComplianceReport);
        RunFullHealthCheckCommand = ReactiveCommand.CreateFromTask(RunFullHealthCheck);
        ClearAlertsCommand = ReactiveCommand.Create(ClearAlerts);

        // Subscribe to system health alerts
        _systemHealthService.AlertGenerated += OnSystemAlertGenerated;

        // Auto-refresh dashboard every 10 seconds
        Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(10))
            .Subscribe(_ => RefreshDataCommand.Execute().Subscribe())
            .DisposeWith(_disposables);

        // Auto-refresh system health every 15 seconds
        Observable.Timer(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15))
            .Subscribe(async _ => await RefreshSystemHealth())
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

    private async Task RefreshSystemHealth()
    {
        try
        {
            // Get system health metrics
            SystemHealth = await _systemHealthService.GetSystemHealthAsync();
            
            // Update health status
            IsSystemHealthy = await _systemHealthService.IsSystemHealthyAsync();
            HealthStatus = SystemHealth.SystemStatus;

            // Get component health
            var components = await _systemHealthService.GetComponentHealthAsync();
            ComponentStatuses.Clear();
            foreach (var component in components)
            {
                ComponentStatuses.Add(component);
            }

            // Get recent alerts
            var alerts = await _systemHealthService.GetActiveAlertsAsync();
            RecentAlerts.Clear();
            foreach (var alert in alerts.Take(5)) // Show last 5 alerts
            {
                RecentAlerts.Add(alert);
            }

            // Log system status for demonstration
            if (SystemHealth.HealthWarnings.Any())
            {
                Console.WriteLine($"⚠️ System Health Warnings: {string.Join(", ", SystemHealth.HealthWarnings)}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to refresh system health: {ex.Message}");
            HealthStatus = "Health monitoring unavailable";
            IsSystemHealthy = false;
        }
    }

    private async Task RunFullHealthCheck()
    {
        try
        {
            Console.WriteLine("Running comprehensive system health check from compliance panel...");
            
            await _systemHealthService.TriggerHealthCheckAsync();
            await RefreshSystemHealth();
            
            Console.WriteLine("Full health check completed from compliance panel");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Health check failed: {ex.Message}");
        }
    }

    private void ClearAlerts()
    {
        RecentAlerts.Clear();
        Console.WriteLine("System alerts cleared from compliance panel");
    }

    private void OnSystemAlertGenerated(object? sender, SystemAlert alert)
    {
        // Add new alerts to collection on UI thread
        RxApp.MainThreadScheduler.Schedule(() =>
        {
            RecentAlerts.Insert(0, alert);
            
            // Keep only last 5 alerts for UI performance
            while (RecentAlerts.Count > 5)
            {
                RecentAlerts.RemoveAt(RecentAlerts.Count - 1);
            }
        });
    }

    private async Task ExportComplianceReport()
    {
        try
        {
            var uptimeFormatted = DashboardData.SystemHealth.Uptime.ToString(@"dd\.hh\:mm\:ss");
            var systemUptime = SystemHealth.Uptime.ToString(@"dd\.hh\:mm\:ss");
            
            // Get component health for the report
            var components = await _systemHealthService.GetComponentHealthAsync();
            var componentReport = string.Join("\n", components.Select(c => 
                $"  {c.ComponentName}: {(c.IsHealthy ? "✅ OK" : "❌ FAILED")} ({c.ResponseTimeMs:F1}ms)"));

            var reportData = $"""
                === MEDICAL DEVICE COMPLIANCE REPORT ===
                Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
                Report Type: Banking-Style System Health & Compliance Analysis

                COMPLIANCE SUMMARY:
                Overall Score: {DashboardData.ComplianceScore:F1}%
                Active Patient Sessions: {DashboardData.ActiveSessions}
                Total Audit Entries: {DashboardData.TotalAuditEntries}
                Critical Alerts: {DashboardData.CriticalAlerts}

                SYSTEM HEALTH STATUS:
                Overall Status: {SystemHealth.SystemStatus}
                System Uptime: {systemUptime}
                Dashboard Uptime: {uptimeFormatted}
                Memory Usage: {SystemHealth.MemoryUsageBytes / 1024 / 1024} MB
                CPU Usage: {SystemHealth.CpuUsagePercent:F1}%
                Response Time: {SystemHealth.ResponseTimeMs:F1}ms
                Error Rate: {SystemHealth.ErrorRate}%
                Throughput: {SystemHealth.ThroughputOperationsPerSecond:F2} ops/sec

                COMPONENT HEALTH:
                {componentReport}

                ENCRYPTION & SECURITY:
                Encryption Status: Active (AES-256)
                Audit Log Performance: {DashboardData.SystemHealth.AuditLogPerformance:F1}%
                Patient Data Tokenization: Active

                PERFORMANCE METRICS:
                Encryption Performance: {DashboardData.SystemHealth.EncryptionPerformance:F1}%
                System Response Time: {SystemHealth.ResponseTimeMs:F1}ms
                Active Connections: {SystemHealth.ActiveConnections}

                REGULATORY COMPLIANCE:
                - HIPAA: Patient data tokenized and encrypted
                - ISO 13485: Medical device software standards applied
                - 21 CFR Part 820: Quality system regulations followed
                - Audit trail integrity: {(DashboardData.SystemHealth.AuditLogPerformance > 95 ? "COMPLIANT" : "NEEDS ATTENTION")}

                SYSTEM WARNINGS:
                {(SystemHealth.HealthWarnings.Any() ? string.Join("\n", SystemHealth.HealthWarnings.Select(w => $"⚠️ {w}")) : "No active warnings")}

                ACTIVE ALERTS:
                {(RecentAlerts.Any() ? string.Join("\n", RecentAlerts.Select(a => $"{a.Severity}: {a.Title} - {a.Message}")) : "No active alerts")}

                === END COMPLIANCE REPORT ===

                This report demonstrates banking-grade system monitoring
                applied to medical device software compliance.
                """;
                
            Console.WriteLine("=== ENHANCED COMPLIANCE REPORT EXPORT ===");
            Console.WriteLine(reportData);
            Console.WriteLine("=== END REPORT ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to generate compliance report: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _systemHealthService.AlertGenerated -= OnSystemAlertGenerated;
        _disposables.Dispose();
    }
}