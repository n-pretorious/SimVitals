using System;

namespace SimVitals.Converters;

public class SubtractPaddingConverter
{
  public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
  {
    if (value is double width && parameter is string paddingStr)
    {
      if (double.TryParse(paddingStr, out double padding))
      {
        return width - padding; // Subtract the padding from the width
      }
    }
    return value; // Return original value if conversion fails
  }

  public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
  {
    throw new NotImplementedException("ConvertBack is not supported for SubtractPaddingConverter.");
  } 
}