using System;

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