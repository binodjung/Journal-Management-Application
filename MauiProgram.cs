using JournalApplication.Data;
using JournalApplication.Services;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;

namespace JournalApplication
{
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
                });

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();

            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IJournalService, JournalService>();
            builder.Services.AddDbContext<AppDbContext>();
            builder.Services.AddSingleton<UserSessionService>();
            builder.Services.AddMudServices();

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var app = builder.Build();
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();

#endif
            return builder.Build();
        }
    }
}
