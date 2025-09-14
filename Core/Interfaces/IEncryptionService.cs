using System.Threading.Tasks;

namespace Core.Interfaces;

public interface IEncryptionService
{
  Task<string> EncryptAsync(string plaintext);
  Task<string> DecryptAsync(string ciphertext);
  string ComputeHash(string input);
  bool VerifyHash(string input, string hash);
  string GenerateSalt();
}