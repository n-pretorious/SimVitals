using System.Security.Cryptography;
using System.Text;
using Core.Interfaces;

namespace Infrastructure.Security;

public class MedicalEncryptionService : IEncryptionService
{
  private readonly byte[] _encryptionKey = DeriveKeyFromPassphrase("SimVitals-Medical-2024");

  public Task<string> EncryptAsync(string plaintext)
  {
    using var aes = Aes.Create();
    aes.Key = _encryptionKey;
    aes.Mode = CipherMode.CBC;
    aes.Padding = PaddingMode.PKCS7;
    aes.GenerateIV();
    
    using var encryptor = aes.CreateEncryptor();
    var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
    var cipherTextBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);
    
    var result = new byte[aes.IV.Length + cipherTextBytes.Length];
    Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
    Array.Copy(cipherTextBytes, 0, result, aes.IV.Length, cipherTextBytes.Length);

    return Task.FromResult(Convert.ToBase64String(result));
  }

  public Task<string> DecryptAsync(string ciphertext)
  {
    var ciphertextBytes = Convert.FromBase64String(ciphertext);
    
    using var aes = Aes.Create();
    aes.Key = _encryptionKey;
    aes.Mode = CipherMode.CBC;
    aes.Padding = PaddingMode.PKCS7;
    
    var iv = new byte[aes.IV.Length];
    var encrypted = new byte[ciphertextBytes.Length - iv.Length];
    
    Array.Copy(ciphertextBytes, 0, iv, 0, iv.Length);
    Array.Copy(ciphertextBytes, iv.Length, encrypted, 0, encrypted.Length);
    
    aes.IV = iv;
    
    using var decryptor = aes.CreateDecryptor();
    var decryptedBytes = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
    
    return Task.FromResult(Encoding.UTF8.GetString(decryptedBytes));
  }

  public string ComputeHash(string input)
  {
    var salt = "SimVitals-Salt-2024"u8.ToArray();
    var inputBytes = Encoding.UTF8.GetBytes(input);
    var combined = new byte[inputBytes.Length + salt.Length];
        
    Array.Copy(inputBytes, 0, combined, 0, inputBytes.Length);
    Array.Copy(salt, 0, combined, inputBytes.Length, salt.Length);
        
    using var sha256 = SHA256.Create();
    var hash = sha256.ComputeHash(combined);
    return Convert.ToHexString(hash);
  }

  public bool VerifyHash(string input, string hash)
  {
    var computedHash = ComputeHash(input);
    return computedHash.Equals(hash, StringComparison.OrdinalIgnoreCase);
  }

  public string GenerateSalt()
  {
    using var rng = RandomNumberGenerator.Create();
    var saltBytes = new byte[32];
    rng.GetBytes(saltBytes);
    return Convert.ToBase64String(saltBytes);
  }
  
  private static byte[] DeriveKeyFromPassphrase(string passphrase)
  {
    var salt = "SimVitals-Medical-Salt"u8.ToArray();
    using var pbkdf2 = new Rfc2898DeriveBytes(passphrase, salt, 100000, HashAlgorithmName.SHA256);
    return pbkdf2.GetBytes(32); // 256-bit key
  }
}