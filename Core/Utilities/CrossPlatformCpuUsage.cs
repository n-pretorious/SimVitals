using System.Diagnostics;

namespace Core.Utilities;

public sealed class CrossPlatformCpuUsage
{
  private TimeSpan _lastTotalProcessorTime;
  private DateTime _lastCheck;
  private readonly Process _process = Process.GetCurrentProcess();

  public CrossPlatformCpuUsage()
  {
    _lastTotalProcessorTime = _process.TotalProcessorTime;
    _lastCheck = DateTime.UtcNow;
  }

  public double GetCpuUsagePercent()
  {
    var now = DateTime.UtcNow;
    var elapsed = now - _lastCheck;

    var newTotalProcessorTime = _process.TotalProcessorTime;
    var cpuUsedMs = (newTotalProcessorTime - _lastTotalProcessorTime).TotalMilliseconds;
    var cpuUsage = cpuUsedMs / (Environment.ProcessorCount * elapsed.TotalMilliseconds) * 100;

    _lastCheck = now;
    _lastTotalProcessorTime = newTotalProcessorTime;

    return Math.Clamp(cpuUsage, 0, 100);
  }
}