namespace Core.Models;

public class Patient
{
  public Guid Id { get; set; } = Guid.NewGuid();
  public string FirstName { get; set; } = string.Empty;
  public string LastName { get; set; } = string.Empty;
  public DateTime DateOfBirth { get; set; }
  public string MedicalRecordNumber { get; set; } = string.Empty;
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
  public string FullName => $"{FirstName} {LastName}";
  public int Age => DateTime.Now.Year - DateOfBirth.Year;
}

public class EncryptedPatientSession
{
  public PatientToken Token { get; set; } = null!;
  public string EncryptedPatientData { get; set; } = string.Empty;
  public string CreatedBy { get; set; } = string.Empty;
  public DateTime CreatedAt { get; set; }
  public DateTime LastAccessedAt { get; set; }
  public string IntegrityHash { get; set; } = string.Empty;
}