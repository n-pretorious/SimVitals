using System.Text.Json;
using Core.Enums;
using Core.Interfaces;
using Core.Models;

namespace Core.Services;

public class PatientDataService : IPatientDataService
{
  private readonly IEncryptionService _encryptionService;
  private readonly IAuditLogger _auditLogger;
  private readonly Dictionary<string, EncryptedPatientSession> _activeSessions = new();
  
  public PatientDataService(IEncryptionService encryptionService, IAuditLogger auditLogger)
  {
    _encryptionService = encryptionService;
    _auditLogger = auditLogger;
  }
  
  public async Task<PatientToken> CreatePatientSessionAsync(Patient patient, string userId)
  {
    var token = PatientToken.Generate();
    
    var patientJson = JsonSerializer.Serialize(patient);
    var encryptedData = await _encryptionService.EncryptAsync(patientJson);
    
    var integrityHash = _encryptionService.ComputeHash($"{token.Value}:{encryptedData}:{userId}");
    
    var session = new EncryptedPatientSession
    {
      Token = token,
      EncryptedPatientData = encryptedData,
      CreatedBy = userId,
      CreatedAt = DateTime.UtcNow,
      LastAccessedAt = DateTime.UtcNow,
      IntegrityHash = integrityHash
    };
    
    _activeSessions[token.Value] = session;
    
    await _auditLogger.LogAsync(new AuditEntry
    {
      Action = "PATIENT_SESSION_CREATED",
      UserId = userId,
      PatientToken = token.Value,
      Details = $"Encrypted session created for patient {patient.FullName}",
      ComplianceFlags = ComplianceFlags.HIPAA | ComplianceFlags.ISO13485
    });
        
    return token;
  }

  public async Task<Patient?> GetPatientByTokenAsync(PatientToken token)
  {
    if (token.IsExpired || !_activeSessions.TryGetValue(token.Value, out var session))
    {
      return null;
    }
    
    try
    {
      var expectedHash = _encryptionService.ComputeHash(
        $"{token.Value}:{session.EncryptedPatientData}:{session.CreatedBy}");
            
      if (!_encryptionService.VerifyHash(
            $"{token.Value}:{session.EncryptedPatientData}:{session.CreatedBy}", 
            session.IntegrityHash))
      {
        await _auditLogger.LogAsync(new AuditEntry
        {
          Action = "DATA_INTEGRITY_VIOLATION",
          UserId = session.CreatedBy,
          PatientToken = token.Value,
          Severity = AuditSeverity.Critical,
          Details = "Patient data integrity check failed"
        });
        
        return null;
      }
            
      // Decrypt patient data
      var decryptedJson = await _encryptionService.DecryptAsync(session.EncryptedPatientData);
      var patient = JsonSerializer.Deserialize<Patient>(decryptedJson);
            
      // Update access time
      session.LastAccessedAt = DateTime.UtcNow;
            
      return patient;
    }
    catch (Exception ex)
    {
      await _auditLogger.LogAsync(new AuditEntry
      {
        Action = "PATIENT_DATA_DECRYPTION_FAILED",
        PatientToken = token.Value,
        Severity = AuditSeverity.Error,
        Details = ex.Message
      });
      
      return null;
    }
  }

  public Task<List<EncryptedPatientSession>> GetActiveSessionsAsync()
  {
    var activeSessions = _activeSessions.Values
      .Where(s => !s.Token.IsExpired)
      .OrderByDescending(s => s.LastAccessedAt)
      .ToList();
    
    return Task.FromResult(activeSessions);
  }

  public async Task EndSessionAsync(PatientToken token, string userId)
  {
    if (_activeSessions.Remove(token.Value))
    {
      await _auditLogger.LogAsync(new AuditEntry
      {
        Action = "PATIENT_SESSION_TERMINATED",
        UserId = userId,
        PatientToken = token.Value,
        Details = "Session ended by user request",
        ComplianceFlags = ComplianceFlags.HIPAA
      });
    }
  }

  public async Task<bool> ValidateTokenAsync(PatientToken token)
  {
    if (token.IsExpired)
    {
      await _auditLogger.LogAsync(new AuditEntry
      {
        Action = "EXPIRED_TOKEN_ACCESS_ATTEMPT",
        PatientToken = token.Value,
        Severity = AuditSeverity.Warning,
        Details = "Attempt to access expired patient token"
      });
      
      return false;
    }
    
    return _activeSessions.ContainsKey(token.Value);
  }
}