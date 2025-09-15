using System.Collections.Concurrent;
using Core.Enums;

namespace Core.Models;

public class PerformanceTracker(int maxMetrics = 1000)
{
  private readonly ConcurrentQueue<PerformanceMetric> _recentMetrics = new();
  private readonly object _lock = new();

  public void RecordOperation(string operationName, TimeSpan duration, bool isSuccess = true,
    string? additionalInfo = null)
  {
    var metric = new PerformanceMetric
    {
      OperationName = operationName,
      Duration = duration,
      Timestamp = DateTime.UtcNow,
      IsSuccess = isSuccess,
      AdditionalInfo = additionalInfo
    };

    _recentMetrics.Enqueue(metric);

    while (_recentMetrics.Count > maxMetrics)
    {
      _recentMetrics.TryDequeue(out _);
    }
  }

  public PerformanceReport GetPerformanceReport(string? operationName = null, TimeSpan? timeWindow = null)
  {
    var metrics = GetFilteredMetrics(operationName, timeWindow);

    if (!metrics.Any())
    {
      return new PerformanceReport
      {
        OperationName = operationName ?? "All Operations",
        TotalOperations = 0,
        AverageResponseTimeMs = 0,
        MedianResponseTimeMs = 0,
        Percentile95Ms = 0,
        Percentile99Ms = 0,
        SuccessRate = 100,
        ThroughputOperationsPerSecond = 0
      };
    }

    var durations = metrics.Select(m => m.Duration.TotalMilliseconds).OrderBy(d => d).ToList();
    var successfulOperations = metrics.Count(m => m.IsSuccess);
    var timeSpan = metrics.Max(m => m.Timestamp) - metrics.Min(m => m.Timestamp);

    return new PerformanceReport
    {
      OperationName = operationName ?? "All Operations",
      TotalOperations = metrics.Count,
      AverageResponseTimeMs = durations.Average(),
      MedianResponseTimeMs = GetPercentile(durations, 50),
      Percentile95Ms = GetPercentile(durations, 95),
      Percentile99Ms = GetPercentile(durations, 99),
      SuccessRate = (double)successfulOperations / metrics.Count * 100,
      ThroughputOperationsPerSecond = timeSpan.TotalSeconds > 0 ? metrics.Count / timeSpan.TotalSeconds : 0,
      TimeWindow = timeSpan,
      SloViolations = durations.Count(d => d > 1000) // SLA: < 1 second response time
    };
  }

  public List<string> GetSlowOperations(double thresholdMs = 100)
  {
    var slowOperations = new List<string>();

    var metrics = _recentMetrics.ToArray()
      .Where(m => m.Duration.TotalMilliseconds > thresholdMs)
      .GroupBy(m => m.OperationName)
      .OrderByDescending(g => g.Average(m => m.Duration.TotalMilliseconds));

    foreach (var group in metrics)
    {
      var avgMs = group.Average(m => m.Duration.TotalMilliseconds);
      slowOperations.Add($"{group.Key}: {avgMs:F1}ms avg");
    }

    return slowOperations;
  }

  public List<PerformanceAlert> GetPerformanceAlerts()
  {
    var alerts = new List<PerformanceAlert>();
    var recentMetrics = GetFilteredMetrics(timeWindow: TimeSpan.FromMinutes(5));

    // Check for high error rates (like banking fraud detection)
    var errorRate = recentMetrics.Any()
      ? (1.0 - (double)recentMetrics.Count(m => m.IsSuccess) / recentMetrics.Count) * 100
      : 0;
    if (errorRate > 5)
    {
      alerts.Add(new PerformanceAlert
      {
        Severity = AlertSeverity.Error,
        Message = $"High error rate: {errorRate:F1}% (last 5 minutes)",
        Metric = "ErrorRate",
        Value = errorRate,
        Threshold = 5
      });
    }

    // Check for slow response times
    if (recentMetrics.Any())
    {
      var avgResponseTime = recentMetrics.Average(m => m.Duration.TotalMilliseconds);
      if (avgResponseTime > 500)
      {
        alerts.Add(new PerformanceAlert
        {
          Severity = avgResponseTime > 1000 ? AlertSeverity.Critical : AlertSeverity.Warning,
          Message = $"Slow average response time: {avgResponseTime:F1}ms",
          Metric = "ResponseTime",
          Value = avgResponseTime,
          Threshold = 500
        });
      }
    }

    // Check for low throughput
    var report = GetPerformanceReport(timeWindow: TimeSpan.FromMinutes(1));
    if (report.ThroughputOperationsPerSecond < 0.1 && report.TotalOperations > 0)
    {
      alerts.Add(new PerformanceAlert
      {
        Severity = AlertSeverity.Warning,
        Message = $"Low throughput: {report.ThroughputOperationsPerSecond:F2} ops/sec",
        Metric = "Throughput",
        Value = report.ThroughputOperationsPerSecond,
        Threshold = 0.1
      });
    }

    return alerts;
  }

  private List<PerformanceMetric> GetFilteredMetrics(string? operationName = null, TimeSpan? timeWindow = null)
  {
    var metrics = _recentMetrics.ToArray().AsEnumerable();

    if (!string.IsNullOrEmpty(operationName))
    {
      metrics = metrics.Where(m => m.OperationName.Equals(operationName, StringComparison.OrdinalIgnoreCase));
    }

    if (timeWindow.HasValue)
    {
      var cutoff = DateTime.UtcNow - timeWindow.Value;
      metrics = metrics.Where(m => m.Timestamp >= cutoff);
    }

    return metrics.ToList();
  }

  private static double GetPercentile(List<double> sortedValues, int percentile)
  {
    if (!sortedValues.Any()) return 0;

    var index = (percentile / 100.0) * (sortedValues.Count - 1);
    var lowerIndex = (int)Math.Floor(index);
    var upperIndex = (int)Math.Ceiling(index);

    if (lowerIndex == upperIndex)
    {
      return sortedValues[lowerIndex];
    }

    var weight = index - lowerIndex;
    return sortedValues[lowerIndex] * (1 - weight) + sortedValues[upperIndex] * weight;
  }
}