using Bunit;
using Flowbite.Services;

namespace Papra.Companion.Tests.Components;

/// <summary>
/// Base class for all Blazor component tests. Registers services required by
/// Flowbite UI components (TwMerge, FloatingService, etc.) so individual test
/// constructors don't have to repeat the setup.
/// </summary>
public abstract class ComponentTestBase : BunitContext
{
    protected ComponentTestBase()
    {
        Services.AddFlowbite();
    }
}
