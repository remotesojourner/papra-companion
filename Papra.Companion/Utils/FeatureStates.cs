using Papra.Companion.Models;

namespace Papra.Companion.Utils;

public static class FeatureStates
{
    public static FeatureState TitleGeneration(PipelineSettings settings) =>
        settings.IsConfigured ? FeatureState.On : FeatureState.NeedsSetup;

    public static FeatureState EmailAttachments(EmailAttachmentSettings settings) =>
        !settings.IsConfigured ? FeatureState.NeedsSetup : settings.Enabled ? FeatureState.On : FeatureState.Off;
}
