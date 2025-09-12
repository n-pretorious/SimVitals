namespace Core.Models;

public class VitalSigns
{
  public int HeartRate { get; set; } = 72;
  public BloodPressure BloodPressure { get; set; } = new();
  public int RespiratoryRate { get; set; } = 16;
  public double Temperature { get; set; } = 98.6;
  public int OxygenSaturation { get; set; } = 98;
  public DateTime Timestamp { get; set; } = DateTime.UtcNow;

  public VitalSigns Clone()
  {
    return new VitalSigns
    {
      HeartRate = HeartRate,
      BloodPressure = new BloodPressure { Systolic = BloodPressure.Systolic, Diastolic = BloodPressure.Diastolic },
      RespiratoryRate = RespiratoryRate,
      Temperature = Temperature,
      OxygenSaturation = OxygenSaturation,
      Timestamp = DateTime.UtcNow
    };
  }
}

public class BloodPressure
{
  public int Systolic { get; set; } = 120;
  public int Diastolic { get; set; } = 80;
    
  public override string ToString() => $"{Systolic}/{Diastolic}";
}