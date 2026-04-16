using System.Windows;
using JulschaVehicleTool.App.ViewModels;
using JulschaVehicleTool.App.Views;
using JulschaVehicleTool.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace JulschaVehicleTool.App;

public partial class App : Application
{
    private IServiceProvider _serviceProvider = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = new MainWindow
        {
            DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>()
        };
        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Core Services
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<MetaXmlService>();
        services.AddSingleton<BinaryFileService>();
        services.AddSingleton<MeshConversionService>();
        services.AddSingleton<FiveMExportService>();
        services.AddSingleton<MetaImportMatchService>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<ModelViewerViewModel>();
        services.AddSingleton<HandlingEditorViewModel>();
        services.AddSingleton<CarVariationsViewModel>();
        services.AddSingleton<SirenEditorViewModel>();
        services.AddSingleton<VehicleSirenAssignViewModel>();
        services.AddSingleton<VehicleMetaViewModel>();
        services.AddSingleton<ResourceSettingsViewModel>();
        services.AddSingleton<WelcomeViewModel>();
    }
}
