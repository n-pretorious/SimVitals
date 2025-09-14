using System.Security.Cryptography;
using System.Text;

namespace Core.Models;

public class PatientToken
{
  private PatientToken(string value, DateTime createdAt, DateTime expiresAt)
  {
    Value = value;
    CreatedAt = createdAt;
    ExpiresAt = expiresAt;
  }
  
  public string Value { get; set; }
  
  public DateTime CreatedAt { get; set; }
  
  public DateTime ExpiresAt { get; set; }
  
  public bool IsExpired => ExpiresAt < DateTime.UtcNow;
  
  public static PatientToken Generate(TimeSpan? expiration = null)
  {
    var created = DateTime.UtcNow;
    var expires = created.Add(expiration ?? TimeSpan.FromHours(8));
    
    using var rng = RandomNumberGenerator.Create();
    var randomBytes = new byte[16];
    rng.GetBytes(randomBytes);
        
    var tokenValue = $"PT_{Convert.ToHexString(randomBytes)[..12]}_{created:yyyyMMddHHmmss}";
        
    return new PatientToken(tokenValue, created, expires);
  }
  
  public string ComputeHash()
  {
    using var sha256 = SHA256.Create();
    var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(Value));
    return Convert.ToHexString(hash);
  }
}