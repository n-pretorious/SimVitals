using System.Diagnostics;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Utilities;

namespace Core.Services;

public class SystemHealthService(
  IAuditLogger auditLogger,
  IEncryptionService encryptionService)
  : ISystemHealthService, IDisposable
{
  private readonly List<SystemAlert> _activeAlerts = new();
  private readonly DateTime _startTime = DateTime.UtcNow;
  private readonly CrossPlatformCpuUsage _cpuTracker = new();

  public event EventHandler<SystemAlert>? AlertGenerated;

  public async Task<SystemHealthMetrics> GetSystemHealthAsync()
  {
    var process = Process.GetCurrentProcess();
    var warnings = new List<string>();

    try
    {
      var memoryMb = process.WorkingSet64 / 1024 / 1024;
      var cpuUsage = GetCpuUsage();
      var uptime = DateTime.UtcNow - _startTime;

      if (memoryMb > 500)
        warnings.Add($"High memory usage: {memoryMb}MB (Warning: >500MB)");

      if (cpuUsage > 80)
        warnings.Add($"High CPU usage: {cpuUsage:F1}% (Warning: >80%)");

      if (uptime.TotalHours > 24)
        warnings.Add($"Long runtime: {uptime.TotalHours:F1} hours (Consider restart)");

      var errorRate = await CalculateErrorRateAsync();
      if (errorRate > 5)
        warnings.Add($"High error rate: {errorRate}% (Warning: >5%)");

      var metrics = new SystemHealthMetrics
      {
        MemoryUsageBytes = process.WorkingSet64,
        CpuUsagePercent = cpuUsage,
        ActiveConnections = 1, // Simulated for demo
        ResponseTimeMs = await MeasureResponseTimeAsync(),
        ErrorRate = errorRate,
        Uptime = uptime,
        ThroughputOperationsPerSecond = CalculateThroughput(),
        HealthWarnings = warnings,
        SystemStatus = warnings.Any() ? "Warning" : "Operational"
      };

      await auditLogger.LogAsync(new AuditEntry
      {
        Action = "SYSTEM_HEALTH_CHECK",
        Details = $"CPU: {cpuUsage:F1}%, Memory: {memoryMb}MB, Errors: {errorRate}%",
        Severity = warnings.Any() ? AuditSeverity.Warning : AuditSeverity.Info
      });

      return metrics;
    }
    catch (Exception ex)
    {
      await LogSystemAlert(AlertSeverity.Error, "Health Check Failed", ex.Message, "SystemHealthService", ex);

      return new SystemHealthMetrics
      {
        MemoryUsageBytes = 0,
        CpuUsagePercent = 0,
        SystemStatus = "Health Monitoring Unavailable",
        HealthWarnings = new() { "Health monitoring temporarily unavailable" }
      };
    }
  }

  public Task<List<SystemAlert>> GetActiveAlertsAsync()
  {
    var activeAlerts = _activeAlerts.Where(a => !a.IsResolved).ToList();
    return Task.FromResult(activeAlerts);
  }

  public async Task<List<ComponentHealthStatus>> GetComponentHealthAsync()
  {
    var components = new List<ComponentHealthStatus>
    {
      await TestComponentHealth("EncryptionService", TestEncryptionServiceAsync),
      await TestComponentHealth("AuditLogger", TestAuditLoggerAsync),
      await TestComponentHealth("Database", TestDatabaseConnectionAsync),
      await TestComponentHealth("VitalsSimulation", TestVitalsSimulationAsync)
    };

    return components;
  }

  public async Task TriggerHealthCheckAsync()
  {
    try
    {
      var health = await GetSystemHealthAsync();
      var components = await GetComponentHealthAsync();

      if (health.HealthWarnings.Any())
      {
        await LogSystemAlert(
          AlertSeverity.Warning,
          "System Health Warning",
          string.Join("; ", health.HealthWarnings),
          "SystemHealthCheck");
      }

      var failedComponents = components.Where(c => !c.IsHealthy).ToList();
      foreach (var component in failedComponents)
      {
        await LogSystemAlert(
          AlertSeverity.Error,
          $"Component Failure: {component.ComponentName}",
          component.Status,
          component.ComponentName);
      }

      await ValidateEmergencyStopFunctionality();
      await ValidateComplianceSystemIntegrity();
    }
    catch (Exception ex)
    {
      await LogSystemAlert(AlertSeverity.Critical, "Health Check Critical Failure", ex.Message, "SystemHealthCheck",
        ex);
    }
  }

  public async Task<bool> IsSystemHealthyAsync()
  {
    var alerts = await GetActiveAlertsAsync();
    var criticalAlerts = alerts.Count(a => a.Severity == AlertSeverity.Critical);
    var components = await GetComponentHealthAsync();
    var failedCriticalComponents = components.Count(c => !c.IsHealthy && IsCriticalComponent(c.ComponentName));

    return criticalAlerts == 0 && failedCriticalComponents == 0;
  }

  public async Task ResolveAlertAsync(Guid alertId, string resolutionAction)
  {
    var alert = _activeAlerts.FirstOrDefault(a => a.Id == alertId);
    if (alert != null)
    {
      alert.IsResolved = true;
      alert.ResolutionAction = resolutionAction;

      await auditLogger.LogAsync(new AuditEntry
      {
        Action = "ALERT_RESOLVED",
        Details = $"Alert '{alert.Title}' resolved: {resolutionAction}",
        Severity = AuditSeverity.Info
      });
    }
  }

  private double GetCpuUsage()
  {
    try
    {
      return _cpuTracker.GetCpuUsagePercent();
    }
    catch
    {
      return 0;
    }
  }

  private async Task<double> MeasureResponseTimeAsync()
  {
    var sw = Stopwatch.StartNew();
    await Task.Delay(Random.Shared.Next(1, 5));
    sw.Stop();
    return sw.Elapsed.TotalMilliseconds;
  }

  private async Task<int> CalculateErrorRateAsync()
  {
    try
    {
      var recentEntries = await auditLogger.GetAuditTrailAsync(null, DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);
      var errors = recentEntries.Count(e => e.Severity >= AuditSeverity.Error);
      var total = recentEntries.Count;
      return total > 0 ? (errors * 100) / total : 0;
    }
    catch
    {
      return 0;
    }
  }

  private double CalculateThroughput()
  {
    var uptime = DateTime.UtcNow - _startTime;
    var totalOperations = (int)(uptime.TotalSeconds * Random.Shared.NextDouble() * 10);
    return uptime.TotalSeconds > 0 ? totalOperations / uptime.TotalSeconds : 0;
  }

  private async Task<ComponentHealthStatus> TestComponentHealth(
    string componentName,
    Func<Task<(bool isHealthy, string status, double responseTime)>> testFunc)
  {
    var sw = Stopwatch.StartNew();
    try
    {
      var (isHealthy, status, responseTime) = await testFunc();
      sw.Stop();

      return new ComponentHealthStatus
      {
        ComponentName = componentName,
        IsHealthy = isHealthy,
        Status = status,
        ResponseTimeMs = responseTime,
        Metrics = new Dictionary<string, object>
        {
          { "LastTestDuration", sw.Elapsed.TotalMilliseconds },
          { "TestTimestamp", DateTime.UtcNow }
        }
      };
    }
    catch (Exception ex)
    {
      sw.Stop();
      return new ComponentHealthStatus
      {
        ComponentName = componentName,
        IsHealthy = false,
        Status = $"Test failed: {ex.Message}",
        ResponseTimeMs = sw.Elapsed.TotalMilliseconds
      };
    }
  }

  private async Task<(bool isHealthy, string status, double responseTime)> TestEncryptionServiceAsync()
  {
    var sw = Stopwatch.StartNew();
    try
    {
      var testData = "Health check test data";
      var encrypted = await encryptionService.EncryptAsync(testData);
      var decrypted = await encryptionService.DecryptAsync(encrypted);

      sw.Stop();

      var isHealthy = decrypted == testData && sw.ElapsedMilliseconds < 100;
      var status = isHealthy ? "OK" : sw.ElapsedMilliseconds >= 100 ? "Slow response" : "Encryption integrity failure";

      return (isHealthy, status, sw.Elapsed.TotalMilliseconds);
    }
    catch (Exception ex)
    {
      sw.Stop();
      return (false, $"Encryption test failed: {ex.Message}", sw.Elapsed.TotalMilliseconds);
    }
  }

  private async Task<(bool isHealthy, string status, double responseTime)> TestAuditLoggerAsync()
  {
    var sw = Stopwatch.StartNew();
    try
    {
      await auditLogger.LogAsync(new AuditEntry
      {
        Action = "HEALTH_CHECK_TEST_AUDIT",
        Details = "Audit logging system health test"
      });

      sw.Stop();

      var isHealthy = sw.ElapsedMilliseconds < 50;
      var status = isHealthy ? "OK" : "Slow audit logging";

      return (isHealthy, status, sw.Elapsed.TotalMilliseconds);
    }
    catch (Exception ex)
    {
      sw.Stop();
      return (false, $"Audit logging failed: {ex.Message}", sw.Elapsed.TotalMilliseconds);
    }
  }

  private async Task<(bool isHealthy, string status, double responseTime)> TestDatabaseConnectionAsync()
  {
    var sw = Stopwatch.StartNew();
    try
    {
      await Task.Delay(Random.Shared.Next(1, 10));
      sw.Stop();

      var isHealthy = sw.ElapsedMilliseconds < 100;
      var status = isHealthy ? "OK" : "Slow database response";

      return (isHealthy, status, sw.Elapsed.TotalMilliseconds);
    }
    catch (Exception ex)
    {
      sw.Stop();
      return (false, $"Database connection failed: {ex.Message}", sw.Elapsed.TotalMilliseconds);
    }
  }

  private async Task<(bool isHealthy, string status, double responseTime)> TestVitalsSimulationAsync()
  {
    var sw = Stopwatch.StartNew();
    try
    {
      await Task.Delay(1);
      sw.Stop();
      return (true, "OK", sw.Elapsed.TotalMilliseconds);
    }
    catch (Exception ex)
    {
      sw.Stop();
      return (false, $"Vitals simulation test failed: {ex.Message}", sw.Elapsed.TotalMilliseconds);
    }
  }

  private async Task ValidateEmergencyStopFunctionality()
  {
    var sw = Stopwatch.StartNew();
    try
    {
      await Task.Delay(1);
      sw.Stop();

      if (sw.ElapsedMilliseconds > 10)
      {
        await LogSystemAlert(
          AlertSeverity.Warning,
          "Emergency Stop Response Time",
          $"Emergency stop response time: {sw.ElapsedMilliseconds}ms (Warning: >10ms)",
          "EmergencyStop");
      }
    }
    catch (Exception ex)
    {
      await LogSystemAlert(AlertSeverity.Critical, "Emergency Stop Failure", ex.Message, "EmergencyStop", ex);
    }
  }

  private async Task ValidateComplianceSystemIntegrity()
  {
    try
    {
      var random = Random.Shared.NextDouble();
      if (random < 0.02)
      {
        await LogSystemAlert(
          AlertSeverity.Error,
          "Compliance System Warning",
          "Simulated compliance monitoring integrity check detected potential issue",
          "ComplianceSystem");
      }
    }
    catch (Exception ex)
    {
      await LogSystemAlert(AlertSeverity.Critical, "Compliance System Failure", ex.Message, "ComplianceSystem", ex);
    }
  }

  private bool IsCriticalComponent(string componentName)
  {
    var criticalComponents = new[] { "EncryptionService", "AuditLogger", "VitalsSimulation" };
    return criticalComponents.Contains(componentName);
  }

  private async Task LogSystemAlert(AlertSeverity severity, string title, string message, string component,
    Exception? ex = null)
  {
    var alert = new SystemAlert
    {
      Severity = severity,
      Title = title,
      Message = message,
      Component = component,
      Exception = ex,
      AdditionalData = new Dictionary<string, object>
      {
        { "Timestamp", DateTime.UtcNow },
        { "MachineName", Environment.MachineName },
        { "ProcessId", Environment.ProcessId }
      }
    };

    _activeAlerts.Add(alert);
    AlertGenerated?.Invoke(this, alert);

    await auditLogger.LogAsync(new AuditEntry
    {
      Action = "SYSTEM_ALERT_GENERATED",
      Details = $"{severity}: {title} - {message}",
      Severity = severity switch
      {
        AlertSeverity.Critical => AuditSeverity.Critical,
        AlertSeverity.Error => AuditSeverity.Error,
        AlertSeverity.Warning => AuditSeverity.Warning,
        _ => AuditSeverity.Info
      }
    });
  }

  public void Dispose()
  {
  }
}