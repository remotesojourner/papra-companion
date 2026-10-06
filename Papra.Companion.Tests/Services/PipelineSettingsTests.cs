using Papra.Companion.Models;

namespace Papra.Companion.Tests.Services;

public class PipelineSettingsTests
{
    [Fact]
    public void IsConfiguredWhenAllRequiredFieldsSetReturnsTrue()
    {
        var settings = new PipelineSettings
        {
            PapraBaseUrl  = "https://papra.example.com",
            PapraApiToken = "token123",
            OpenAiApiKey  = "sk-abc"
        };

        Assert.True(settings.IsConfigured);
    }

    [Theory]
    [InlineData("", "token", "sk-key")]
    [InlineData("https://papra.example.com", "", "sk-key")]
    [InlineData("https://papra.example.com", "token", "")]
    public void IsConfiguredWhenAnyRequiredFieldMissingReturnsFalse(
        string baseUrl, string token, string openAiKey)
    {
        var settings = new PipelineSettings
        {
            PapraBaseUrl  = baseUrl,
            PapraApiToken = token,
            OpenAiApiKey  = openAiKey
        };

        Assert.False(settings.IsConfigured);
    }

    [Fact]
    public void DefaultTitlePromptContainsOriginalTitlePlaceholder()
    {
        Assert.Contains("{{original_title}}", PipelineSettings.DefaultTitlePrompt);
    }

    [Fact]
    public void ProcessingDelaySecondsDefaultsToZero()
    {
        var settings = new PipelineSettings();
        Assert.Equal(0, settings.ProcessingDelaySeconds);
    }
}
