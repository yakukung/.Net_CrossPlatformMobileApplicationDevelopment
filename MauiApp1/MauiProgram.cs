using Microsoft.Extensions.Logging;

namespace MauiApp1;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<Services.DataService>();
        builder.Services.AddTransient<ViewModels.ProfilePageViewModel>();
        builder.Services.AddTransient<ViewModels.HomePageViewModel>();

        builder.Services.AddTransient<Views.ProfilePage>();
        builder.Services.AddTransient<Views.HomePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
