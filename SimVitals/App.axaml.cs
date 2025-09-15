using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Core.Interfaces;
using Core.Services;
using Infrastructure.Compliance;
using Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;
using SimVitals.ViewModels;
using SimVitals.Views;

namespace SimVitals;

public partial class App : Application
{
  public override void Initialize()
  {
    AvaloniaXamlLoader.Load(this);
  }

  public override void OnFrameworkInitializationCompleted()
  {
    var services = new ServiceCollection();
    
    services.AddSingleton<IEncryptionService, MedicalEncryptionService>();
    services.AddSingleton<IAuditLogger, MedicalAuditLogger>();
    services.AddSingleton<IPatientDataService, PatientDataService>();
    services.AddSingleton<IComplianceService, ComplianceService>();
    services.AddSingleton<IComplianceDashboardService, ComplianceDashboardService>();
    services.AddSingleton<IScenarioService, ScenarioService>();
    services.AddScoped<ISystemHealthService, SystemHealthService>();
    
    services.AddTransient<CompliancePanelViewModel>();
    services.AddTransient<MainWindowViewModel>();
    
    
    var serviceProvider = services.BuildServiceProvider();
    
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
      desktop.MainWindow = new MainWindow
      {
        DataContext = serviceProvider.GetRequiredService<MainWindowViewModel>()
      };
    }

    base.OnFrameworkInitializationCompleted();
  }
}