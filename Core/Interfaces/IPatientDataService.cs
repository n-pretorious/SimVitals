using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Models;

namespace Core.Interfaces;

public interface IPatientDataService
{
  Task<PatientToken> CreatePatientSessionAsync(Patient patient, string userId);
  Task<Patient?> GetPatientByTokenAsync(PatientToken token);
  Task<List<EncryptedPatientSession>> GetActiveSessionsAsync();
  Task EndSessionAsync(PatientToken token, string userId);
  Task<bool> ValidateTokenAsync(PatientToken token);
}