using ColourIdentifier.Controls;
using ColourIdentifier.Platforms.Android;

namespace ColourIdentifier;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>()
            .ConfigureMauiHandlers(handlers => handlers.AddHandler<ImageSampler, ImageSamplerHandler>());
        builder.Services.AddSingleton<MainPage>();
        return builder.Build();
    }
}
