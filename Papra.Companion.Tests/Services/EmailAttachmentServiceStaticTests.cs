using Papra.Companion.Constants;
using Papra.Companion.Models;
using Papra.Companion.Services;

namespace Papra.Companion.Tests.Services;

public class EmailAttachmentServiceStaticTests
{
    [Fact]
    public void BuildSubjectRegexWhenNoPatternReturnsNull()
    {
        var settings = new EmailAttachmentSettings { SubjectRegex = string.Empty };
        Assert.Null(EmailAttachmentService.BuildSubjectRegex(settings));
    }

    [Fact]
    public void BuildSubjectRegexDefaultSettingsAnchoredToStart()
    {
        var settings = new EmailAttachmentSettings
        {
            SubjectRegex = "Invoice",
            SubjectRegexMatchAnywhere = false,
            SubjectRegexIgnoreCase = false,
        };
        var regex = EmailAttachmentService.BuildSubjectRegex(settings)!;

        Assert.Matches(regex, "Invoice 2024");
        Assert.DoesNotMatch(regex, "Re: Invoice 2024");
    }

    [Fact]
    public void BuildSubjectRegexMatchAnywhereMatchesMidString()
    {
        var settings = new EmailAttachmentSettings
        {
            SubjectRegex = "Invoice",
            SubjectRegexMatchAnywhere = true,
            SubjectRegexIgnoreCase = false,
        };
        var regex = EmailAttachmentService.BuildSubjectRegex(settings)!;

        Assert.Matches(regex, "Re: Invoice 2024");
    }

    [Fact]
    public void BuildSubjectRegexCaseInsensitiveMatchesRegardlessOfCase()
    {
        var settings = new EmailAttachmentSettings
        {
            SubjectRegex = "invoice",
            SubjectRegexIgnoreCase = true,
            SubjectRegexMatchAnywhere = true,
        };
        var regex = EmailAttachmentService.BuildSubjectRegex(settings)!;

        Assert.Matches(regex, "INVOICE 123");
        Assert.Matches(regex, "Invoice 123");
    }

    [Fact]
    public void BuildSubjectRegexCaseSensitiveDoesNotMatchWrongCase()
    {
        var settings = new EmailAttachmentSettings
        {
            SubjectRegex = "invoice",
            SubjectRegexIgnoreCase = false,
            SubjectRegexMatchAnywhere = true,
        };
        var regex = EmailAttachmentService.BuildSubjectRegex(settings)!;

        Assert.DoesNotMatch(regex, "INVOICE 123");
        Assert.Matches(regex, "invoice 123");
    }

    [Fact]
    public void SanitizePathValidFilenameReturnsSameValue()
    {
        var result = EmailAttachmentService.SanitizePath("valid-filename.pdf");
        Assert.Equal("valid-filename.pdf", result);
    }

    [Fact]
    public void SanitizePathInvalidCharsReplacedWithUnderscore()
    {
        var result = EmailAttachmentService.SanitizePath("file:name/with\\bad*chars?.pdf");
        Assert.DoesNotContain(":", result);
        Assert.DoesNotContain("/", result);
        Assert.DoesNotContain("\\", result);
        Assert.DoesNotContain("*", result);
        Assert.DoesNotContain("?", result);
    }

    [Fact]
    public void BuildSavePathNoTemplateUsesAttachmentName()
    {
        var settings = new EmailAttachmentSettings { FilenameTemplate = string.Empty };
        var date = new DateTimeOffset(2024, 3, 15, 0, 0, 0, TimeSpan.Zero);

        var path = EmailAttachmentService.BuildSavePath(
            settings, "/app", "msg001", "receipt.pdf", "Subject", "from@example.com", date);

        Assert.Equal(Path.Combine("/app", AppPaths.AttachmentsFolder, "receipt.pdf"), path);
    }

    [Fact]
    public void BuildSavePathWithTemplateInterpolatesAllPlaceholders()
    {
        var settings = new EmailAttachmentSettings
        {
            FilenameTemplate = "{{date}}-{{subject}}-{{attachment_name}}"
        };
        var date = new DateTimeOffset(2024, 3, 15, 0, 0, 0, TimeSpan.Zero);

        var path = EmailAttachmentService.BuildSavePath(
            settings, "/app", "msg001", "receipt.pdf", "My Invoice", "from@example.com", date);

        var filename = Path.GetFileName(path);
        Assert.Equal("2024-03-15-My Invoice-receipt.pdf", filename);
    }

    [Fact]
    public void BuildSavePathWithTemplateSpacedPlaceholdersAlsoWork()
    {
        var settings = new EmailAttachmentSettings
        {
            FilenameTemplate = "{{ date }}-{{ attachment_name }}"
        };
        var date = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);

        var path = EmailAttachmentService.BuildSavePath(
            settings, "/app", "msg002", "doc.pdf", "Subject", "a@b.com", date);

        var filename = Path.GetFileName(path);
        Assert.Equal("2024-06-01-doc.pdf", filename);
    }

    [Fact]
    public void BuildSavePathWithTemplateMessageIdAndFromEmailInterpolated()
    {
        var settings = new EmailAttachmentSettings
        {
            FilenameTemplate = "{{message_id}}-{{from_email}}-{{attachment_name}}"
        };
        var date = DateTimeOffset.UtcNow;

        var path = EmailAttachmentService.BuildSavePath(
            settings, "/app", "unique-id-123", "file.pdf", "Subject", "sender@test.com", date);

        var filename = Path.GetFileName(path);
        Assert.Equal("unique-id-123-sender@test.com-file.pdf", filename);
    }

    [Fact]
    public void BuildSavePathAttachmentNameWithInvalidCharsSanitized()
    {
        var settings = new EmailAttachmentSettings { FilenameTemplate = string.Empty };
        var date = DateTimeOffset.UtcNow;

        var path = EmailAttachmentService.BuildSavePath(
            settings, "/app", "msg", "bad:name?.pdf", "Sub", "a@b.com", date);

        var filename = Path.GetFileName(path);
        Assert.DoesNotContain(":", filename);
        Assert.DoesNotContain("?", filename);
    }

    [Fact]
    public void BuildSavePathIsUnderAttachmentsFolder()
    {
        var settings = new EmailAttachmentSettings { FilenameTemplate = string.Empty };

        var path = EmailAttachmentService.BuildSavePath(
            settings, "/app", "msg", "file.pdf", "Sub", "a@b.com", DateTimeOffset.UtcNow);

        Assert.StartsWith(Path.Combine("/app", AppPaths.AttachmentsFolder), path);
    }
}
