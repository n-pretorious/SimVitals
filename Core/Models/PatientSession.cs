namespace Core.Models;

public class PatientSession
{
  public string PatientToken { get; set; } = string.Empty;
  public string PatientName { get; set; } = string.Empty;
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public string CreatedBy { get; set; } = string.Empty;
  public bool IsActive { get; set; } = true;
  public string SessionDuration => (DateTime.UtcNow - CreatedAt).ToString(@"mm\:ss");
}