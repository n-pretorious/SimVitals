using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Core.Enums;
using Core.Interfaces;
using ReactiveUI;
using Core.Models;


namespace SimVitals.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly IPatientDataService _patientService;
    private readonly IComplianceService _complianceService;
    private readonly ISystemHealthService _systemHealthService;
    
    private readonly CompositeDisposable _dispose = new();
    private readonly Random _random = new();
    private readonly PerformanceTracker _performanceTracker = new();

    // ReactiveUI Source Generator properties
    private VitalSigns _currentVitals = new();
    public VitalSigns CurrentVitals
    {
        get => _currentVitals;
        private set => this.RaiseAndSetIfChanged(ref _currentVitals, value);
    }
    
    private PatientSession _currentSession = new();
    public PatientSession CurrentSession
    {
        get => _currentSession;
        private set => this.RaiseAndSetIfChanged(ref _currentSession, value);
    }
    
    private string _currentScenario = "Normal Vitals";
    public string CurrentScenario
    {
        get => _currentScenario;
        set => this.RaiseAndSetIfChanged(ref _currentScenario, value);
    }
    
    private string _heartRateStatus = "Normal";
    public string HeartRateStatus
    {
        get => _heartRateStatus;
        set => this.RaiseAndSetIfChanged(ref _heartRateStatus, value);
    }
    
    private string _systemStatus = "All Systems Operational";
    public string SystemStatus
    {
        get => _systemStatus;
        set => this.RaiseAndSetIfChanged(ref _systemStatus, value);
    }

    private double _averageResponseTimeMs = 0;
    public double AverageResponseTimeMs
    {
        get => _averageResponseTimeMs;
        set => this.RaiseAndSetIfChanged(ref _averageResponseTimeMs, value);
    }

    private long _systemMemoryMb;
    public long SystemMemoryMb
    {
        get => _systemMemoryMb;
        set => this.RaiseAndSetIfChanged(ref _systemMemoryMb, value);
    }
    
    private string _performanceStatus = "Optimal";
    public string PerformanceStatus
    {
        get => _performanceStatus;
        set => this.RaiseAndSetIfChanged(ref _performanceStatus, value);
    }
    
    private bool _isAnimating = false;
    public bool IsAnimating
    {
        get => _isAnimating;
        set => this.RaiseAndSetIfChanged(ref _isAnimating, value);
    }
    
    private string _animationStatus = "Stable";
    public string AnimationStatus
    {
        get => _animationStatus;
        set => this.RaiseAndSetIfChanged(ref _animationStatus, value);
    }
    
    // Add these ECG-related properties to your MainWindowViewModel

    private bool _isEcgAlarmActive = false;
    public bool IsEcgAlarmActive
    {
        get => _isEcgAlarmActive;
        set => this.RaiseAndSetIfChanged(ref _isEcgAlarmActive, value);
    }

    private string _ecgRhythmType = "Sinus Rhythm";
    public string EcgRhythmType
    {
        get => _ecgRhythmType;
        set => this.RaiseAndSetIfChanged(ref _ecgRhythmType, value);
    }

    private string _ecgSignalQuality = "Excellent";
    public string EcgSignalQuality
    {
        get => _ecgSignalQuality;
        set => this.RaiseAndSetIfChanged(ref _ecgSignalQuality, value);
    }

    private string _ecgRhythmAnalysis = "Regular rhythm, normal intervals";
    public string EcgRhythmAnalysis
    {
        get => _ecgRhythmAnalysis;
        set => this.RaiseAndSetIfChanged(ref _ecgRhythmAnalysis, value);
    }

    private string _ecgAlarmStatus = "No active alarms";
    public string EcgAlarmStatus
    {
        get => _ecgAlarmStatus;
        set => this.RaiseAndSetIfChanged(ref _ecgAlarmStatus, value);
    }

    private string _ecgSignalQualityDescription = "Lead contact: Good";
    public string EcgSignalQualityDescription
    {
        get => _ecgSignalQualityDescription;
        set => this.RaiseAndSetIfChanged(ref _ecgSignalQualityDescription, value);
    }

// ECG intervals (medical parameters)
    private int _ecgPRInterval = 160;
    public int EcgPRInterval
    {
        get => _ecgPRInterval;
        set => this.RaiseAndSetIfChanged(ref _ecgPRInterval, value);
    }

    private int _ecgQRSWidth = 80;
    public int EcgQRSWidth
    {
        get => _ecgQRSWidth;
        set => this.RaiseAndSetIfChanged(ref _ecgQRSWidth, value);
    }

    private int _ecgQTInterval = 400;
    public int EcgQTInterval
    {
        get => _ecgQTInterval;
        set => this.RaiseAndSetIfChanged(ref _ecgQTInterval, value);
    }

    private int _ecgRRInterval = 833;
    public int EcgRRInterval
    {
        get => _ecgRRInterval;
        set => this.RaiseAndSetIfChanged(ref _ecgRRInterval, value);
    }
    
    public ObservableCollection<AuditEntry> AuditEntries { get; }
    public ObservableCollection<SystemAlert> SystemAlerts { get; } = new();
    
    public CompliancePanelViewModel CompliancePanel { get; }

    // ReactiveUI Commands
    public ReactiveCommand<Unit, Unit> TriggerCardiacEventCommand { get; }
    public ReactiveCommand<Unit, Unit> TriggerAnaphylaxisCommand { get; }
    public ReactiveCommand<Unit, Unit> TriggerStrokeCommand { get; }
    public ReactiveCommand<Unit, Unit> NormalizeVitalsCommand { get; }
    public ReactiveCommand<Unit, Unit> EmergencyStopCommand { get; }
    public ReactiveCommand<Unit, Unit> TestSafetySystemCommand { get; }
    public ReactiveCommand<Unit, Unit> RunSystemHealthCheckCommand { get; }
    public ReactiveCommand<Unit, Unit> GetPerformanceReportCommand { get; }


    public MainWindowViewModel(
        IPatientDataService patientService,
        IComplianceService complianceService,
        IComplianceDashboardService dashboardService,
        ISystemHealthService systemHealthService)
    {
        _patientService = patientService;
        _complianceService = complianceService;
        _systemHealthService = systemHealthService;
        
        CompliancePanel = new CompliancePanelViewModel(dashboardService, systemHealthService);
        
        AuditEntries = new ObservableCollection<AuditEntry>();
        
        // Initialize commands
        TriggerCardiacEventCommand = ReactiveCommand.CreateFromTask(TriggerCardiacEvent);
        TriggerAnaphylaxisCommand = ReactiveCommand.CreateFromTask(TriggerAnaphylaxis);
        TriggerStrokeCommand = ReactiveCommand.CreateFromTask(TriggerStroke);
        NormalizeVitalsCommand = ReactiveCommand.CreateFromTask(NormalizeVitals);
        EmergencyStopCommand = ReactiveCommand.CreateFromTask(EmergencyStop);
        TestSafetySystemCommand = ReactiveCommand.CreateFromTask(TestSafetySystem);
        RunSystemHealthCheckCommand = ReactiveCommand.CreateFromTask(RunSystemHealthCheck);
        GetPerformanceReportCommand = ReactiveCommand.Create(GetPerformanceReport);
        
        _systemHealthService.AlertGenerated += OnSystemAlertGenerated;
        
        _ = InitializePatientSessionAsync();
        
        AddInitialAuditEntries();

        // Start real-time vitals simulation using Observable
        Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(2))
            .SelectMany(async _ =>
            {
                await UpdateVitals();
                return Unit.Default;
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe()
            .DisposeWith(_dispose);
        
        // Add system monitoring (every 10 seconds)
        Observable.Timer(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10))
            .SelectMany(async _ =>
            {
                await UpdateSystemMetrics();
                return Unit.Default;
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe()
            .DisposeWith(_dispose);
    }
    
    
    private async Task InitializePatientSessionAsync()
    {
        var patient = new Patient
        {
            FirstName = "Training",
            LastName = "Patient",
            DateOfBirth = DateTime.Now.AddYears(-45),
            MedicalRecordNumber = "MRN-DEMO-001"
        };
        
        try
        {
            var token = await _patientService.CreatePatientSessionAsync(patient, "demo-instructor");
            
            CurrentSession = new PatientSession
            {
                PatientToken = token.Value,
                PatientName = "Training Simulation",
                CreatedBy = "Dr. Sarah Johnson",
                CreatedAt = DateTime.UtcNow
            };
            
            Console.WriteLine($"Created encrypted patient session: {token.Value}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to create patient session: {ex.Message}");
        }
    }
    
    private async Task AnimateVitalsTo(VitalSigns targetVitals, TimeSpan duration, string statusMessage)
    {
        IsAnimating = true;
        AnimationStatus = statusMessage;
    
        var steps = 30; // More steps for smoother animation
        var stepDelay = (int)(duration.TotalMilliseconds / steps);
    
        var startVitals = CurrentVitals.Clone();
    
        for (int i = 1; i <= steps; i++)
        {
            var progress = (double)i / steps;
        
            var newVitals = new VitalSigns
            {
                HeartRate = (int)Math.Round(startVitals.HeartRate + (targetVitals.HeartRate - startVitals.HeartRate) * progress),
                BloodPressure = new BloodPressure
                {
                    Systolic = (int)Math.Round(startVitals.BloodPressure.Systolic + (targetVitals.BloodPressure.Systolic - startVitals.BloodPressure.Systolic) * progress),
                    Diastolic = (int)Math.Round(startVitals.BloodPressure.Diastolic + (targetVitals.BloodPressure.Diastolic - startVitals.BloodPressure.Diastolic) * progress)
                },
                OxygenSaturation = (int)Math.Round(startVitals.OxygenSaturation + (targetVitals.OxygenSaturation - startVitals.OxygenSaturation) * progress),
                RespiratoryRate = (int)Math.Round(startVitals.RespiratoryRate + (targetVitals.RespiratoryRate - startVitals.RespiratoryRate) * progress),
                Timestamp = DateTime.UtcNow
            };
        
            CurrentVitals = newVitals;
        
            // Update heart rate status during animation
            HeartRateStatus = newVitals.HeartRate switch
            {
                < 60 => "Bradycardia - Below Normal",
                > 100 => "Tachycardia - Above Normal",
                _ => "Normal Range: 60-100 BPM"
            };
        
            await Task.Delay(stepDelay);
        }
    
        IsAnimating = false;
        AnimationStatus = "Stable";
    }

    private async Task UpdateVitals()
    {
        if (IsAnimating) return; // Don't interfere with scenario animations

        var sw = Stopwatch.StartNew();

        try
        {
            var newVitals = CurrentVitals.Clone();

            // Your existing random variations for normal state
            if (CurrentScenario == "Normal Vitals")
            {
                newVitals.HeartRate += _random.Next(-2, 3);
                newVitals.HeartRate = Math.Max(60, Math.Min(100, newVitals.HeartRate));

                // Continue with other vitals...
                newVitals.BloodPressure.Systolic += _random.Next(-1, 2);
                newVitals.BloodPressure.Systolic = Math.Max(110, Math.Min(130, newVitals.BloodPressure.Systolic));

                newVitals.BloodPressure.Diastolic += _random.Next(-1, 2);
                newVitals.BloodPressure.Diastolic = Math.Max(70, Math.Min(90, newVitals.BloodPressure.Diastolic));

                newVitals.OxygenSaturation += _random.Next(-1, 2);
                newVitals.OxygenSaturation = Math.Max(95, Math.Min(100, newVitals.OxygenSaturation));

                newVitals.RespiratoryRate += _random.Next(-1, 2);
                newVitals.RespiratoryRate = Math.Max(12, Math.Min(20, newVitals.RespiratoryRate));
            }

            newVitals.Timestamp = DateTime.UtcNow;

            HeartRateStatus = newVitals.HeartRate switch
            {
                < 60 => "Bradycardia - Below Normal",
                > 100 => "Tachycardia - Above Normal",
                _ => "Normal Range: 60-100 BPM"
            };

            var isValid = await _complianceService.ValidateVitalsSafetyAsync(newVitals, CurrentSession.PatientToken);

            if (isValid)
            {
                CurrentVitals = newVitals;
                SystemStatus = "All Systems Operational";
            }
            else
            {
                SystemStatus = "⚠️ Safety Alert - Simulation Halted";
                AddAuditEntry("SAFETY_VIOLATION", "Vitals exceeded safe parameters - simulation halted");
                await NormalizeVitals();
            }

            _performanceTracker.RecordOperation("VitalsUpdate", sw.Elapsed, true);
        }
        catch (Exception ex)
        {
            _performanceTracker.RecordOperation("VitalsUpdate", sw.Elapsed, false, ex.Message);
            throw;
        }
    }

    private async Task TriggerCardiacEvent()
    {
        CurrentScenario = "Cardiac Arrest";
    
        // Update ECG parameters for cardiac arrest
        EcgRhythmType = "Ventricular Fibrillation";
        EcgRhythmAnalysis = "Chaotic rhythm, no discernible P waves";
        IsEcgAlarmActive = true;
        EcgAlarmStatus = "CRITICAL: V-Fib detected";
        EcgSignalQuality = "Poor - Artifact";
    
        AddAuditEntry("SCENARIO_TRIGGERED", "Instructor triggered: CARDIAC_ARREST");
    
        var targetVitals = new VitalSigns
        {
            HeartRate = 0,
            BloodPressure = new BloodPressure { Systolic = 0, Diastolic = 0 },
            OxygenSaturation = 75,
            RespiratoryRate = 0,
            Timestamp = DateTime.UtcNow
        };
    
        await AnimateVitalsTo(targetVitals, TimeSpan.FromSeconds(4), "Cardiac arrest in progress...");
    }

  
    private async Task TriggerAnaphylaxis()
    {
        CurrentScenario = "Anaphylactic Shock";
    
        // ECG changes for anaphylaxis (tachycardia)
        EcgRhythmType = "Sinus Tachycardia";
        EcgRhythmAnalysis = "Fast regular rhythm, normal morphology";
        IsEcgAlarmActive = true;
        EcgAlarmStatus = "HIGH: Heart rate >150 BPM";
        EcgSignalQuality = "Good";
        EcgPRInterval = 120;    // Shorter due to fast rate
        EcgQRSWidth = 80;       // Normal
        EcgQTInterval = 320;    // Shorter due to fast rate
        EcgRRInterval = 400;    // 150 BPM = 400ms intervals
    
        AddAuditEntry("SCENARIO_TRIGGERED", "Instructor triggered: ANAPHYLACTIC_SHOCK");
    
        var targetVitals = new VitalSigns
        {
            HeartRate = 150,
            BloodPressure = new BloodPressure { Systolic = 80, Diastolic = 40 },
            OxygenSaturation = 85,
            RespiratoryRate = 30,
            Timestamp = DateTime.UtcNow
        };
    
        await AnimateVitalsTo(targetVitals, TimeSpan.FromSeconds(3), "Anaphylactic reaction developing...");
    }

    private async Task TriggerStroke()
    {
        CurrentScenario = "Acute Stroke";
    
        // ECG changes for stroke (hypertensive response)
        EcgRhythmType = "Sinus Rhythm";
        EcgRhythmAnalysis = "Regular rhythm, possible LVH changes";
        IsEcgAlarmActive = true;
        EcgAlarmStatus = "WARNING: Hypertensive response";
        EcgSignalQuality = "Good";
        EcgPRInterval = 170;    // Slightly prolonged
        EcgQRSWidth = 100;      // Widened (LVH)
        EcgQTInterval = 440;    // Prolonged
        EcgRRInterval = 632;    // 95 BPM
    
        AddAuditEntry("SCENARIO_TRIGGERED", "Instructor triggered: ACUTE_STROKE");
    
        var targetVitals = new VitalSigns
        {
            HeartRate = 95,
            BloodPressure = new BloodPressure { Systolic = 180, Diastolic = 110 },
            OxygenSaturation = 92,
            RespiratoryRate = 22,
            Timestamp = DateTime.UtcNow
        };
    
        await AnimateVitalsTo(targetVitals, TimeSpan.FromSeconds(2.5), "Stroke symptoms manifesting...");
    }
    
    private async Task NormalizeVitals()
    {
        CurrentScenario = "Normal Vitals";
    
        // Reset ECG to normal
        EcgRhythmType = "Sinus Rhythm";
        EcgRhythmAnalysis = "Regular rhythm, normal intervals";
        IsEcgAlarmActive = false;
        EcgAlarmStatus = "No active alarms";
        EcgSignalQuality = "Excellent";
        EcgPRInterval = 160;
        EcgQRSWidth = 80;
        EcgQTInterval = 400;
        EcgRRInterval = 833;
    
        AddAuditEntry("SCENARIO_NORMALIZED", "Vitals returning to normal ranges");
    
        var targetVitals = new VitalSigns
        {
            HeartRate = 72,
            BloodPressure = new BloodPressure { Systolic = 120, Diastolic = 80 },
            OxygenSaturation = 98,
            RespiratoryRate = 16,
            Timestamp = DateTime.UtcNow
        };
    
        await AnimateVitalsTo(targetVitals, TimeSpan.FromSeconds(6), "Stabilizing patient...");
    }

    private async Task EmergencyStop()
    {
        AddAuditEntry("EMERGENCY_STOP", "CRITICAL: Emergency stop activated by instructor");
        await NormalizeVitals(); // Reset to normal with animation
    }
    
    private async Task TestSafetySystem()
    {
        Console.WriteLine("Testing safety system with extreme values...");
    
        var dangerousVitals = new VitalSigns
        {
            HeartRate = 400, // Extreme value
            BloodPressure = new BloodPressure { Systolic = 350, Diastolic = 200 },
            OxygenSaturation = 150, // Impossible value
            RespiratoryRate = 100,
            Timestamp = DateTime.UtcNow
        };
    
        var isValid = await _complianceService.ValidateVitalsSafetyAsync(
            dangerousVitals, CurrentSession.PatientToken);
    
        Console.WriteLine($"Safety validation result: {isValid}");
    
        if (!isValid)
        {
            AddAuditEntry("SAFETY_TEST", "Deliberately triggered safety violation for testing");
        }
    }

    private void AddAuditEntry(string action, string details)
    {
        var entry = new AuditEntry
        {
            Action = action,
            UserId = "dr.johnson@hospital.edu",
            Details = details,
            PatientToken = CurrentSession.PatientToken
        };

        // Add to beginning of collection
        AuditEntries.Insert(0, entry);
        
        // Keep only last 20 entries
        while (AuditEntries.Count > 20)
        {
            AuditEntries.RemoveAt(AuditEntries.Count - 1);
        }
    }

    private void AddInitialAuditEntries()
    {
        AddAuditEntry("MANIKIN_CONNECTED", "SimMan Essential | SN: SM001-2024");
        AddAuditEntry("PATIENT_TOKEN_CREATED", "system | Encrypted: SHA-256");
        AddAuditEntry("SESSION_STARTED", "dr.johnson | Scenario: Normal");
        AddAuditEntry("INSTRUCTOR_LOGIN", "dr.johnson@hospital.edu");
    }
    
    private async Task UpdateSystemMetrics()
    {
        try
        {
            var health = await _systemHealthService.GetSystemHealthAsync();
            
            SystemMemoryMb = health.MemoryUsageBytes / 1024 / 1024;
            
            // Update performance metrics
            var report = _performanceTracker.GetPerformanceReport("VitalsUpdate");
            AverageResponseTimeMs = report.AverageResponseTimeMs;
            
            // Update performance status based on system health
            PerformanceStatus = health.HealthWarnings.Any() ? "Performance Warning" : "Optimal";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"System metrics update error: {ex.Message}");
        }
    }

    private void OnSystemAlertGenerated(object? sender, SystemAlert alert)
    {
        // Add to UI collection on main thread
        RxApp.MainThreadScheduler.Schedule(() =>
        {
            SystemAlerts.Insert(0, alert);
            
            // Keep only last 10 alerts for UI performance
            while (SystemAlerts.Count > 10)
            {
                SystemAlerts.RemoveAt(SystemAlerts.Count - 1);
            }
            
            // Add to audit trail for critical/error alerts
            if (alert.Severity >= AlertSeverity.Error)
            {
                AddAuditEntry("SYSTEM_ALERT", $"{alert.Severity}: {alert.Title} - {alert.Message}");
            }
        });
    }
    
    private async Task RunSystemHealthCheck()
    {
        var sw = Stopwatch.StartNew();
        
        try
        {
            Console.WriteLine("Running comprehensive system health check...");
            
            await _systemHealthService.TriggerHealthCheckAsync();
            
            var health = await _systemHealthService.GetSystemHealthAsync();
            var components = await _systemHealthService.GetComponentHealthAsync();
            
            sw.Stop();
            
            Console.WriteLine($"System Health Check Complete ({sw.ElapsedMilliseconds}ms):");
            Console.WriteLine($"  CPU Usage: {health.CpuUsagePercent:F1}%");
            Console.WriteLine($"  Memory: {health.MemoryUsageBytes / 1024 / 1024}MB");
            Console.WriteLine($"  System Status: {health.SystemStatus}");
            
            foreach (var component in components)
            {
                var status = component.IsHealthy ? "✅ OK" : "❌ FAILED";
                Console.WriteLine($"    {component.ComponentName}: {status} ({component.ResponseTimeMs:F1}ms)");
            }
            
            AddAuditEntry("SYSTEM_HEALTH_CHECK", $"Manual health check completed in {sw.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            sw.Stop();
            Console.WriteLine($"Health check failed: {ex.Message}");
            AddAuditEntry("HEALTH_CHECK_ERROR", $"Health check failed: {ex.Message}");
        }
    }

    private void GetPerformanceReport()
    {
        var overallReport = _performanceTracker.GetPerformanceReport();
        var vitalsReport = _performanceTracker.GetPerformanceReport("VitalsUpdate");
        
        Console.WriteLine("=== PERFORMANCE REPORT ===");
        Console.WriteLine($"Overall Operations:");
        Console.WriteLine($"  Total Operations: {overallReport.TotalOperations}");
        Console.WriteLine($"  Average Response: {overallReport.AverageResponseTimeMs:F1}ms");
        Console.WriteLine($"  95th Percentile: {overallReport.Percentile95Ms:F1}ms");
        Console.WriteLine($"  Success Rate: {overallReport.SuccessRate:F1}%");
        
        Console.WriteLine($"Vitals Updates:");
        Console.WriteLine($"  Average: {vitalsReport.AverageResponseTimeMs:F1}ms");
        Console.WriteLine($"  Success Rate: {vitalsReport.SuccessRate:F1}%");
        
        AddAuditEntry("PERFORMANCE_REPORT", $"Performance report generated - {overallReport.TotalOperations} operations analyzed");
    }

    
    public void Dispose()
    {
        _dispose.Dispose();
    }
}