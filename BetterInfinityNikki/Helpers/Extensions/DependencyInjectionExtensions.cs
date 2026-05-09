using BetterInfinityNikki.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using BetterInfinityNikki.ViewModel;

namespace BetterInfinityNikki.Helpers.Extensions;

internal static class DependencyInjectionExtensions
{
    public static IServiceCollection AddView<TWindow, TWindowImplementation, TViewModel>(this IServiceCollection services)
        where TWindow : class
        where TWindowImplementation : class, TWindow
        where TViewModel : class, IViewModel
    {
        return services
            .AddSingleton<TWindow, TWindowImplementation>()
            .AddSingleton<TViewModel>();
    }

    public static IServiceCollection AddView<TPage, TViewModel>(this IServiceCollection services)
        where TPage : FrameworkElement
        where TViewModel : class, IViewModel
    {
        return services
            .AddSingleton<TPage>()
            .AddSingleton<TViewModel>();
    }
}
