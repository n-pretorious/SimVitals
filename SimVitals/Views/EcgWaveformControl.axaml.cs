using System;
using System.Collections.Generic;
using Avalonia.Controls.Shapes;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace SimVitals.Views;

public partial class EcgWaveformControl : UserControl
{
  private readonly DispatcherTimer _animationTimer;
  private readonly List<Point> _waveformPoints = new();
  private double _currentX;
  private int _heartRate = 72;
  private string _rhythmType = "Normal";
  private bool _isInitialized;

  // Styled Properties for data binding
  public static readonly StyledProperty<int> HeartRateProperty =
    AvaloniaProperty.Register<EcgWaveformControl, int>(nameof(HeartRate), 72);

  public static readonly StyledProperty<string> RhythmTypeProperty =
    AvaloniaProperty.Register<EcgWaveformControl, string>(nameof(RhythmType), "Normal");

  public int HeartRate
  {
    get => GetValue(HeartRateProperty);
    set => SetValue(HeartRateProperty, value);
  }

  public string RhythmType
  {
    get => GetValue(RhythmTypeProperty);
    set => SetValue(RhythmTypeProperty, value);
  }

  public EcgWaveformControl()
  {
    InitializeComponent();

    _animationTimer = new DispatcherTimer
    {
      Interval = TimeSpan.FromMilliseconds(50) // 20 FPS for smooth animation
    };
    _animationTimer.Tick += AnimationTimer_Tick;

    PropertyChanged += OnPropertyChanged;
    Loaded += OnLoaded;
  }

  private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
  {
    if (!_isInitialized)
    {
      DrawGridLines();
      _animationTimer.Start();
      _isInitialized = true;
    }
  }

  private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
  {
    if (e.Property == HeartRateProperty)
    {
      _heartRate = HeartRate;
    }
    else if (e.Property == RhythmTypeProperty)
    {
      _rhythmType = RhythmType;
    }
  }

  private void DrawGridLines()
  {
    var canvasWidth = EcgCanvas.Bounds.Width;
    var canvasHeight = EcgCanvas.Bounds.Height;

    if (canvasWidth <= 0 || canvasHeight <= 0) return;

    GridCanvas.Children.Clear();

    // Major grid lines (5mm squares) - like real ECG paper
    var majorSpacing = 25; // 5mm at 25mm/s = 25 pixels
    var minorSpacing = 5; // 1mm = 5 pixels

    // Vertical major lines
    for (double x = 0; x <= canvasWidth; x += majorSpacing)
    {
      var line = new Rectangle
      {
        Fill = new SolidColorBrush(Color.Parse("#1A4A3A")),
        Width = 1,
        Height = canvasHeight
      };
      Canvas.SetLeft(line, x);
      GridCanvas.Children.Add(line);
    }

    // Horizontal major lines
    for (double y = 0; y <= canvasHeight; y += majorSpacing)
    {
      var line = new Rectangle
      {
        Fill = new SolidColorBrush(Color.Parse("#1A4A3A")),
        Width = canvasWidth,
        Height = 1
      };
      Canvas.SetTop(line, y);
      GridCanvas.Children.Add(line);
    }

    // Vertical minor lines
    for (double x = minorSpacing; x <= canvasWidth; x += minorSpacing)
    {
      if (x % majorSpacing != 0) // Skip major line positions
      {
        var line = new Rectangle
        {
          Fill = new SolidColorBrush(Color.Parse("#0D2818")),
          Width = 1,
          Height = canvasHeight
        };
        Canvas.SetLeft(line, x);
        GridCanvas.Children.Add(line);
      }
    }

    // Horizontal minor lines
    for (double y = minorSpacing; y <= canvasHeight; y += minorSpacing)
    {
      if (y % majorSpacing != 0) // Skip major line positions
      {
        var line = new Rectangle
        {
          Fill = new SolidColorBrush(Color.Parse("#0D2818")),
          Width = canvasWidth,
          Height = 1
        };
        Canvas.SetTop(line, y);
        GridCanvas.Children.Add(line);
      }
    }
  }

  private void AnimationTimer_Tick(object? sender, EventArgs e)
  {
    UpdateWaveform();
    UpdateSweepLine();
  }

  private void UpdateWaveform()
  {
    var canvasWidth = EcgCanvas.Bounds.Width;
    var canvasHeight = EcgCanvas.Bounds.Height;

    if (canvasWidth <= 0 || canvasHeight <= 0) return;

    // Calculate timing based on heart rate
    var beatsPerSecond = Math.Max(0.1, _heartRate) / 60.0; // Prevent division by zero
    var pixelsPerSecond = 50; // 25mm/s paper speed * 2 for visibility
    var beatWidth = beatsPerSecond > 0 ? pixelsPerSecond / beatsPerSecond : pixelsPerSecond;

    // Generate ECG waveform based on rhythm type
    var baselineY = canvasHeight / 2;
    var newPoint = GenerateEcgPoint(_currentX, baselineY, beatWidth);

    _waveformPoints.Add(newPoint);

    // Remove old points that are off-screen
    var screenBuffer = 100; // Keep some points off-screen for smooth transitions
    while (_waveformPoints.Count > 0 && _waveformPoints[0].X < _currentX - canvasWidth - screenBuffer)
    {
      _waveformPoints.RemoveAt(0);
    }

    // Shift points to create scrolling effect
    var visiblePoints = _waveformPoints
      .Select(p => new Point(p.X - (_currentX - canvasWidth), p.Y))
      .Where(p => p.X >= -screenBuffer && p.X <= canvasWidth + screenBuffer)
      .ToList();

    EcgWaveform.Points = visiblePoints;

    _currentX += 2; // Move forward 2 pixels each frame
  }

  private Point GenerateEcgPoint(double x, double baselineY, double beatWidth)
  {
    var y = baselineY;

    if (_heartRate == 0)
    {
      // Flatline for cardiac arrest
      y = baselineY;
    }
    else if (_rhythmType == "Ventricular Fibrillation")
    {
      // Chaotic waveform for V-Fib
      y += (Random.Shared.NextDouble() - 0.5) * 60;
    }
    else
    {
      // Generate normal ECG waveform - SAME AMPLITUDE FOR ALL RHYTHMS
      var beatPosition = (x % beatWidth) / beatWidth;

      // P wave (0.08 - 0.12 seconds)
      if (beatPosition >= 0.05 && beatPosition < 0.15)
      {
        var pPosition = (beatPosition - 0.05) / 0.1;
        y -= Math.Sin(pPosition * Math.PI) * 8;
      }
      // QRS complex (0.06 - 0.10 seconds)
      else if (beatPosition >= 0.2 && beatPosition < 0.3)
      {
        var qrsPosition = (beatPosition - 0.2) / 0.1;
        if (qrsPosition < 0.2) // Q wave
        {
          y += Math.Sin(qrsPosition * 5 * Math.PI) * 6;
        }
        else if (qrsPosition < 0.7) // R wave
        {
          var rPosition = (qrsPosition - 0.2) / 0.5;
          y -= Math.Sin(rPosition * Math.PI) * 50;
        }
        else // S wave
        {
          var sPosition = (qrsPosition - 0.7) / 0.3;
          y += Math.Sin(sPosition * Math.PI) * 15;
        }
      }
      // T wave (0.16 - 0.24 seconds)
      else if (beatPosition >= 0.4 && beatPosition < 0.7)
      {
        var tPosition = (beatPosition - 0.4) / 0.3;
        y -= Math.Sin(tPosition * Math.PI) * 20;
      }

      // Add slight random variation for realism - CONSISTENT FOR ALL
      y += (Random.Shared.NextDouble() - 0.5) * 1.5;
    }

    return new Point(x, Math.Max(10, Math.Min(baselineY * 2 - 10, y)));
  }

  private void UpdateSweepLine()
  {
    var canvasWidth = EcgCanvas.Bounds.Width;
    if (canvasWidth <= 0) return;

    // Position sweep line at the leading edge
    Canvas.SetLeft(SweepLine, canvasWidth - 50);
  }

  protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
  {
    _animationTimer?.Stop();
    base.OnDetachedFromVisualTree(e);
  }
}