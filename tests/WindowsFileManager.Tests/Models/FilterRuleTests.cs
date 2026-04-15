using System.Text.Json;
using FluentAssertions;
using WindowsFileManager.Core.Models;

namespace WindowsFileManager.Tests.Models;

public class FilterRuleTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var rule = new FilterRule();

        rule.Pattern.Should().BeEmpty();
        rule.IsEnabled.Should().BeTrue();
        rule.IsRegex.Should().BeFalse();
        rule.IgnoreCase.Should().BeTrue();
        rule.Action.Should().Be(FilterAction.Contains);
        rule.Target.Should().Be(FilterTarget.Filename);
    }

    [Fact]
    public void DisplaySummary_ContainsWithFlags_ShouldFormat()
    {
        var rule = new FilterRule
        {
            Pattern = "*.jpg",
            Action = FilterAction.Contains,
            Target = FilterTarget.Filename,
            IsRegex = true,
            IgnoreCase = true,
        };

        rule.DisplaySummary.Should().Be("Contains | Filename | \"*.jpg\" [Regex, IgnoreCase]");
    }

    [Fact]
    public void DisplaySummary_IgnoreNoFlags_ShouldFormat()
    {
        var rule = new FilterRule
        {
            Pattern = "backup",
            Action = FilterAction.Ignore,
            Target = FilterTarget.Filepath,
            IsRegex = false,
            IgnoreCase = false,
        };

        rule.DisplaySummary.Should().Be("Ignore | Filepath | \"backup\"");
    }

    [Fact]
    public void JsonSerialize_ShouldNotIncludeDisplaySummary()
    {
        var rule = new FilterRule { Pattern = "test" };
        var json = JsonSerializer.Serialize(rule);

        json.Should().NotContain("DisplaySummary");
    }

    [Fact]
    public void JsonSerialize_ShouldNotIncludePriority()
    {
        var rule = new FilterRule { Pattern = "test", Priority = 5 };
        var json = JsonSerializer.Serialize(rule);

        json.Should().NotContain("Priority");
    }

    [Fact]
    public void JsonRoundTrip_ShouldPreserveAllProperties()
    {
        var rule = new FilterRule
        {
            Pattern = "photo",
            IsEnabled = false,
            IsRegex = true,
            IgnoreCase = false,
            Action = FilterAction.Ignore,
            Target = FilterTarget.Filepath,
        };

        var json = JsonSerializer.Serialize(rule);
        var deserialized = JsonSerializer.Deserialize<FilterRule>(json)!;

        deserialized.Pattern.Should().Be("photo");
        deserialized.IsEnabled.Should().BeFalse();
        deserialized.IsRegex.Should().BeTrue();
        deserialized.IgnoreCase.Should().BeFalse();
        deserialized.Action.Should().Be(FilterAction.Ignore);
        deserialized.Target.Should().Be(FilterTarget.Filepath);
    }

    [Fact]
    public void JsonDeserialize_MissingIsEnabled_ShouldDefaultToTrue()
    {
        var json = """{"Pattern":"test","IsRegex":false,"IgnoreCase":true,"Action":0,"Target":0}""";
        var rule = JsonSerializer.Deserialize<FilterRule>(json)!;

        rule.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void JsonDeserialize_WithDisplaySummary_ShouldNotFail()
    {
        var json = """{"Pattern":"old","IsRegex":false,"IgnoreCase":true,"Action":0,"Target":0,"DisplaySummary":"Select | Filename | \"old\" [IgnoreCase]"}""";
        var rule = JsonSerializer.Deserialize<FilterRule>(json)!;

        rule.Pattern.Should().Be("old");
        rule.Action.Should().Be(FilterAction.Contains);
    }
}
