namespace Core.Models;

public class EncryptedPatientSession
{
  public PatientToken Token { get; set; } = null!;
  public string EncryptedPatientData { get; set; } = string.Empty;
  public string CreatedBy { get; set; } = string.Empty;
  public DateTime CreatedAt { get; set; }
  public DateTime LastAccessedAt { get; set; }
  public string IntegrityHash { get; set; } = string.Empty;
}