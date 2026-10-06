using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Papra.Companion.Services;

namespace Papra.Companion.Tests.Components;

public abstract class ComponentTestBase : BunitContext
{
    protected ComponentTestBase()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices(options => options.PopoverOptions.CheckForPopoverProvider = false);
        Services.AddScoped<BrowserInteropService>();
    }

    protected static IElement Row<T>(IRenderedComponent<T> cut, string label) where T : IComponent =>
        cut.FindAll(".pc-settings-row").Single(row => row.QuerySelector("h3")?.TextContent.Trim() == label);

    protected static IElement Button<T>(IRenderedComponent<T> cut, string text) where T : IComponent =>
        cut.FindAll("button").Single(button => button.TextContent.Trim() == text);

    protected static IElement Field<T>(IRenderedComponent<T> cut, string label) where T : IComponent
    {
        var labelElement = cut.FindAll("label").First(l => l.TextContent.Trim() == label);
        return cut.Find($"#{labelElement.GetAttribute("for")}");
    }
}
