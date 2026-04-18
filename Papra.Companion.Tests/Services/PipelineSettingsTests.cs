using Papra.Companion.Models;

namespace Papra.Companion.Tests.Services;

public class PipelineSettingsTests
{
    [Fact]
    public void IsConfigured_WhenAllRequiredFieldsSet_ReturnsTrue()
    {
        var settings = new PipelineSettings
        {
            PapraBaseUrl = "https://papra.example.com",
            PapraApiToken = "token123",
            OpenAiApiKey = "sk-abc"
        };

        Assert.True(settings.IsConfigured);
    }

    [Theory]
    [InlineData("", "token", "sk-key")]
    [InlineData("https://papra.example.com", "", "sk-key")]
    [InlineData("https://papra.example.com", "token", "")]
    public void IsConfigured_WhenAnyRequiredFieldMissing_ReturnsFalse(
        string baseUrl, string token, string openAiKey)
    {
        var settings = new PipelineSettings
        {
            PapraBaseUrl = baseUrl,
            PapraApiToken = token,
            OpenAiApiKey = openAiKey
        };

        Assert.False(settings.IsConfigured);
    }

    [Fact]
    public void UseOpenAiForOcr_WhenMistralKeyEmpty_ReturnsTrue()
    {
        var settings = new PipelineSettings { MistralApiKey = string.Empty };
        Assert.True(settings.UseOpenAiForOcr);
    }

    [Fact]
    public void UseOpenAiForOcr_WhenMistralKeySet_ReturnsFalse()
    {
        var settings = new PipelineSettings { MistralApiKey = "mistral-key" };
        Assert.False(settings.UseOpenAiForOcr);
    }

    [Fact]
    public void DefaultPrompts_ContainExpectedPlaceholders()
    {
        Assert.Contains("{{original_title}}", PipelineSettings.DefaultTitlePrompt);
        Assert.Contains("{{content}}", PipelineSettings.DefaultTitlePrompt);

        Assert.Contains("{{available_tags}}", PipelineSettings.DefaultTagPrompt);
        Assert.Contains("{{original_title}}", PipelineSettings.DefaultTagPrompt);
        Assert.Contains("{{content}}", PipelineSettings.DefaultTagPrompt);

        Assert.NotEmpty(PipelineSettings.DefaultOcrPrompt);
    }
}
