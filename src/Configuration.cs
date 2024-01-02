using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Recrovit.RecroGridFramework.Client.Blazor.SyncfusionUI.Components;
using System.Reflection;

namespace Recrovit.RecroGridFramework.Client.Blazor.SyncfusionUI;

public static class RGFClientBlazorSyncfusionConfiguration
{
    public static async Task InitializeRgfSyncfusionUIAsync(this IServiceProvider serviceProvider, string themeName = "bootstrap5", bool loadResources = true)
    {
        RgfBlazorConfiguration.RegisterComponent<MenuComponent>(RgfBlazorConfiguration.ComponentType.Menu);
        RgfBlazorConfiguration.RegisterComponent<DialogComponent>(RgfBlazorConfiguration.ComponentType.Dialog);
        RgfBlazorConfiguration.RegisterEntityComponent<EntityComponent>(string.Empty);

        if (loadResources)
        {
            var jsRuntime = serviceProvider.GetRequiredService<IJSRuntime>();
            await LoadResourcesAsync(jsRuntime, themeName);
        }
    }

    public static async Task LoadResourcesAsync(IJSRuntime jsRuntime, string themeName)
    {
        var libName = Assembly.GetExecutingAssembly().GetName().Name;

        await jsRuntime.InvokeVoidAsync("import", $"{RgfClientConfiguration.AppRootUrl}_content/Syncfusion.Blazor.Core/scripts/syncfusion-blazor.min.js");
        await jsRuntime.InvokeVoidAsync("Recrovit.LPUtils.AddStyleSheetLink", $"{RgfClientConfiguration.AppRootUrl}_content/{libName}/lib/bootstrap/dist/css/bootstrap.min.css", false, BootstrapCssId);
        await jsRuntime.InvokeVoidAsync("Recrovit.LPUtils.AddStyleSheetLink", $"{RgfClientConfiguration.AppRootUrl}_content/Syncfusion.Blazor.Themes/{themeName}.css", false, SyncfusionThemeId);
    }

    public static async Task UnloadResourcesAsync(IJSRuntime jsRuntime)
    {
        await jsRuntime.InvokeVoidAsync("eval", $"document.getElementById('{BootstrapCssId}')?.remove();");
        await jsRuntime.InvokeVoidAsync("eval", $"document.getElementById('{SyncfusionThemeId}')?.remove();");

        var libName = Assembly.GetExecutingAssembly().GetName().Name;
        await jsRuntime.InvokeVoidAsync("Recrovit.LPUtils.RemoveLinkedFile", $"{RgfClientConfiguration.AppRootUrl}_content/{libName}/css/syncfusion-dark-theme.min.css", "stylesheet");
        await jsRuntime.InvokeVoidAsync("eval", "document.getElementsByTagName('body')[0].removeAttribute('class');");
    }

    private static readonly string BootstrapCssId = "syncfusion-bootstrap";
    public static readonly string SyncfusionThemeId = "syncfusion-theme";
}
