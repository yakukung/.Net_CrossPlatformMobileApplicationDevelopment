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
        builder.Services.AddSingleton<Services.ProfileService>();
        builder.Services.AddSingleton<Services.RegisterAndWithdrawCourseService>();

        builder.Services.AddTransient<ViewModels.ProfilePageViewModel>();
        builder.Services.AddTransient<ViewModels.HomePageViewModel>();
        builder.Services.AddTransient<ViewModels.RegistrationPageViewModel>();
        builder.Services.AddTransient<ViewModels.WithdrawPageViewModel>();
        builder.Services.AddTransient<ViewModels.HistoryViewModel>();

        builder.Services.AddTransient<Views.ProfilePage>();
        builder.Services.AddTransient<Views.HomePage>();
        builder.Services.AddTransient<Views.RegistrationPage>();
        builder.Services.AddTransient<Views.WithdrawPage>();
        builder.Services.AddTransient<Views.HistoryPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
