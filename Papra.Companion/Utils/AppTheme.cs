using MudBlazor;

namespace Papra.Companion.Utils;

public static class AppTheme
{
    public const string ThemeStorageKey = "color-theme";

    private const string Primary = "#2563eb";

    private const string PrimaryOnDark = "#3b82f6";

    public static MudTheme Theme { get; } = new()
    {
        PaletteDark = new PaletteDark
        {
            Primary = PrimaryOnDark,
            AppbarBackground = "#0b0b0b",
            AppbarText = "#e5e7eb",
            DrawerBackground = "#0b0b0b",
            Background = "#0b0b0b",
            BackgroundGray = "#222222",
            Surface = "#121212",
            GrayDefault = "#222222",
            LinesInputs = "#333333",
            LinesDefault = "#333333",
            ActionDefault = "#828282",
            TextPrimary = "#e5e7eb",
            TextSecondary = "#9ca3af",
            Success = "#10b981",
            Warning = "#f59e0b",
            Error = "#ef4444",
            Info = "#3b82f6"
        },
        PaletteLight = new PaletteLight
        {
            Primary = Primary,
            AppbarBackground = "#ffffff",
            AppbarText = "#18181b",
            DrawerBackground = "#ffffff",
            DrawerText = "#18181b",
            Background = "#f7f7f8",
            BackgroundGray = "#efeff1",
            Surface = "#ffffff",
            GrayDefault = "#e4e4e7",
            LinesInputs = "#d4d4d8",
            LinesDefault = "#e4e4e7",
            TextPrimary = "#18181b",
            TextSecondary = "#52525b",
            ActionDefault = "#71717a",
            Success = "#10b981",
            Warning = "#f59e0b",
            Error = "#ef4444",
            Info = "#3b82f6"
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px",
            AppbarHeight = "56px"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Inter", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif"]
            },
            H4 = new H4Typography { FontWeight = "700" },
            H5 = new H5Typography { FontWeight = "600" },
            H6 = new H6Typography { FontWeight = "600" },
            Subtitle1 = new Subtitle1Typography { FontWeight = "600" },
            Subtitle2 = new Subtitle2Typography { FontWeight = "600" }
        }
    };
}
