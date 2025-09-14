using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Core.Interfaces;
using ReactiveUI;
using Core.Models;


namespace SimVitals.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IPatientDataService _patientService;
    private readonly IComplianceService _complianceService;
    
    private readonly CompositeDisposable _vitalsTimer = new();
    private readonly Random _random = new();

    // ReactiveUI Source Generator properties
    private VitalSigns _currentVitals = new();
    private PatientSession _currentSession = new();
    private string _currentScenario = "Normal Vitals";
    
    public VitalSigns CurrentVitals
    {
        get => _currentVitals;
        set => this.RaiseAndSetIfChanged(ref _currentVitals, value);
    }

    public PatientSession CurrentSession
    {
        get => _currentSession;
        set => this.RaiseAndSetIfChanged(ref _currentSession, value);
    }

    public string CurrentScenario
    {
        get => _currentScenario;
        set => this.RaiseAndSetIfChanged(ref _currentScenario, value);
    }

    public ObservableCollection<AuditEntry> AuditEntries { get; } = [];

    // ReactiveUI Commands
    public ReactiveCommand<Unit, Unit> TriggerCardiacEventCommand { get; }
    public ReactiveCommand<Unit, Unit> TriggerAnaphylaxisCommand { get; }
    public ReactiveCommand<Unit, Unit> TriggerStrokeCommand { get; }
    public ReactiveCommand<Unit, Task> NormalizeVitalsCommand { get; }
    public ReactiveCommand<Unit, Unit> EmergencyStopCommand { get; }

    public MainWindowViewModel(
        IPatientDataService patientService,
        IComplianceService complianceService,
        IAuditLogger auditLogger)
    {
        _patientService = patientService;
        _complianceService = complianceService;
        
        InitializePatientSession();

        // Initialize reactive commands
        TriggerCardiacEventCommand = ReactiveCommand.Create(TriggerCardiacEvent);
        TriggerAnaphylaxisCommand = ReactiveCommand.Create(TriggerAnaphylaxis);
        TriggerStrokeCommand = ReactiveCommand.Create(TriggerStroke);
        NormalizeVitalsCommand = ReactiveCommand.Create(NormalizeVitals);
        EmergencyStopCommand = ReactiveCommand.Create(EmergencyStop);

        // Add initial audit entries
        AddInitialAuditEntries();

        // Start real-time vitals simulation using Observable
        Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(2))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => UpdateVitals())
            .DisposeWith(_vitalsTimer);
    }
    
    private async void InitializePatientSession()
    {
        // Create a real patient with encryption
        var patient = new Patient
        {
            FirstName = "Training",
            LastName = "Patient",
            DateOfBirth = DateTime.Now.AddYears(-45),
            MedicalRecordNumber = "MRN-DEMO-001"
        };
        
        var token = await _patientService.CreatePatientSessionAsync(patient, "demo-instructor");
        
        CurrentSession = new PatientSession
        {
            PatientToken = token.Value,
            PatientName = "Training Simulation",
            CreatedBy = "Dr. John Doe",
            CreatedAt = DateTime.UtcNow
        };
    }

    private async void UpdateVitals()
    {
        var newVitals = CurrentVitals.Clone();
        
        // Add small random variations for realism
        newVitals.HeartRate += _random.Next(-2, 3);
        newVitals.HeartRate = Math.Max(60, Math.Min(100, newVitals.HeartRate));
        
        newVitals.BloodPressure.Systolic += _random.Next(-1, 2);
        newVitals.BloodPressure.Systolic = Math.Max(110, Math.Min(130, newVitals.BloodPressure.Systolic));
        
        newVitals.BloodPressure.Diastolic += _random.Next(-1, 2);
        newVitals.BloodPressure.Diastolic = Math.Max(70, Math.Min(90, newVitals.BloodPressure.Diastolic));
        
        newVitals.OxygenSaturation += _random.Next(-1, 2);
        newVitals.OxygenSaturation = Math.Max(95, Math.Min(100, newVitals.OxygenSaturation));
        
        newVitals.RespiratoryRate += _random.Next(-1, 2);
        newVitals.RespiratoryRate = Math.Max(12, Math.Min(20, newVitals.RespiratoryRate));
        
        newVitals.Timestamp = DateTime.UtcNow;

        // Update the property (ReactiveUI Source Generator handles PropertyChanged)
        var isValid = await _complianceService.ValidateVitalsSafetyAsync(newVitals, CurrentSession.PatientToken);
        
        if (!isValid)
        {
            CurrentVitals = newVitals;
        }
        else
        {
            // Safety violation - stop simulation
            AddAuditEntry("SAFETY_VIOLATION", "Vitals exceeded safe parameters - simulation halted");
            await NormalizeVitals();
        }
    }

    private void TriggerCardiacEvent()
    {
        CurrentScenario = "Cardiac Arrest";
        
        CurrentVitals = new VitalSigns
        {
            HeartRate = 0,
            BloodPressure = new BloodPressure { Systolic = 0, Diastolic = 0 },
            OxygenSaturation = 75,
            RespiratoryRate = 0,
            Timestamp = DateTime.UtcNow
        };
        
        AddAuditEntry("SCENARIO_TRIGGERED", "Instructor triggered: CARDIAC_ARREST");
    }

    private void TriggerAnaphylaxis()
    {
        CurrentScenario = "Anaphylactic Shock";
        
        CurrentVitals = new VitalSigns
        {
            HeartRate = 150,
            BloodPressure = new BloodPressure { Systolic = 80, Diastolic = 40 },
            OxygenSaturation = 85,
            RespiratoryRate = 30,
            Timestamp = DateTime.UtcNow
        };
        
        AddAuditEntry("SCENARIO_TRIGGERED", "Instructor triggered: ANAPHYLACTIC_SHOCK");
    }

    private void TriggerStroke()
    {
        CurrentScenario = "Acute Stroke";
        
        CurrentVitals = new VitalSigns
        {
            HeartRate = 95,
            BloodPressure = new BloodPressure { Systolic = 180, Diastolic = 110 },
            OxygenSaturation = 92,
            RespiratoryRate = 22,
            Timestamp = DateTime.UtcNow
        };
        
        AddAuditEntry("SCENARIO_TRIGGERED", "Instructor triggered: ACUTE_STROKE");
    }

    private Task NormalizeVitals()
    {
        CurrentScenario = "Normal Vitals";
        
        CurrentVitals = new VitalSigns
        {
            HeartRate = 72,
            BloodPressure = new BloodPressure { Systolic = 120, Diastolic = 80 },
            OxygenSaturation = 98,
            RespiratoryRate = 16,
            Timestamp = DateTime.UtcNow
        };
        
        AddAuditEntry("SCENARIO_NORMALIZED", "Vitals returning to normal ranges");
        
        return Task.CompletedTask;
    }

    private void EmergencyStop()
    {
        AddAuditEntry("EMERGENCY_STOP", "CRITICAL: Emergency stop activated by instructor");
        NormalizeVitals(); // Reset to normal
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

    public void Dispose()
    {
        _vitalsTimer?.Dispose();
    }
}