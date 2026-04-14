using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace WindowsFileManager.Helpers;

/// <summary>
/// Attached behavior that parses simple markup tags into formatted TextBlock inlines.
/// Supported tags: &lt;b&gt;bold&lt;/b&gt;, &lt;h&gt;highlight&lt;/h&gt;, &lt;w&gt;warning&lt;/w&gt;.
/// Newlines (\n) are converted to LineBreak elements.
/// </summary>
[ExcludeFromCodeCoverage]
public static class FormattedTextBehavior
{
    private static readonly SolidColorBrush HighlightForeground = new(Color.FromRgb(0x0D, 0x47, 0xA1));
    private static readonly SolidColorBrush WarningForeground = new(Color.FromRgb(0xC6, 0x28, 0x28));
    private static readonly SolidColorBrush WarningBackground = new(Color.FromRgb(0xFF, 0xEB, 0xEE));

    /// <summary>
    /// Identifies the FormattedText attached property.
    /// </summary>
    public static readonly DependencyProperty FormattedTextProperty =
        DependencyProperty.RegisterAttached(
            "FormattedText",
            typeof(string),
            typeof(FormattedTextBehavior),
            new PropertyMetadata(null, OnFormattedTextChanged));

    /// <summary>
    /// Gets the formatted text for the specified TextBlock.
    /// </summary>
    /// <param name="obj">The dependency object to read from.</param>
    /// <returns>The formatted text string, or null.</returns>
    public static string? GetFormattedText(DependencyObject obj) =>
        (string?)obj.GetValue(FormattedTextProperty);

    /// <summary>
    /// Sets the formatted text for the specified TextBlock.
    /// </summary>
    /// <param name="obj">The dependency object to write to.</param>
    /// <param name="value">The formatted text to parse and display.</param>
    public static void SetFormattedText(DependencyObject obj, string? value) =>
        obj.SetValue(FormattedTextProperty, value);

    private static void OnFormattedTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock textBlock)
        {
            return;
        }

        textBlock.Inlines.Clear();

        if (e.NewValue is not string text || string.IsNullOrEmpty(text))
        {
            return;
        }

        ParseAndApply(textBlock, text);
    }

    private static void ParseAndApply(TextBlock textBlock, string text)
    {
        var position = 0;

        while (position < text.Length)
        {
            var tagStart = text.IndexOf('<', position);

            if (tagStart < 0)
            {
                AddPlainText(textBlock, text[position..]);
                break;
            }

            if (tagStart > position)
            {
                AddPlainText(textBlock, text[position..tagStart]);
            }

            var tagEnd = text.IndexOf('>', tagStart);
            if (tagEnd < 0)
            {
                AddPlainText(textBlock, text[tagStart..]);
                break;
            }

            var tag = text[(tagStart + 1)..tagEnd];

            if (tag is "b" or "h" or "w")
            {
                var closeTag = $"</{tag}>";
                var closePos = text.IndexOf(closeTag, tagEnd + 1, StringComparison.Ordinal);

                if (closePos < 0)
                {
                    AddPlainText(textBlock, text[tagStart..]);
                    break;
                }

                var content = text[(tagEnd + 1)..closePos];
                AddStyledRun(textBlock, content, tag);
                position = closePos + closeTag.Length;
            }
            else
            {
                AddPlainText(textBlock, text[tagStart..(tagEnd + 1)]);
                position = tagEnd + 1;
            }

            continue;
        }
    }

    private static void AddPlainText(TextBlock textBlock, string text)
    {
        var parts = text.Split('\n');
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
            {
                textBlock.Inlines.Add(new Run(parts[i]));
            }

            if (i < parts.Length - 1)
            {
                textBlock.Inlines.Add(new LineBreak());
            }
        }
    }

    private static void AddStyledRun(TextBlock textBlock, string content, string tag)
    {
        var parts = content.Split('\n');
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
            {
                var run = new Run(parts[i]);
                switch (tag)
                {
                    case "b":
                        run.FontWeight = FontWeights.Bold;
                        break;
                    case "h":
                        run.FontWeight = FontWeights.SemiBold;
                        run.Foreground = HighlightForeground;
                        break;
                    case "w":
                        run.FontWeight = FontWeights.SemiBold;
                        run.Foreground = WarningForeground;
                        run.Background = WarningBackground;
                        break;
                }

                textBlock.Inlines.Add(run);
            }

            if (i < parts.Length - 1)
            {
                textBlock.Inlines.Add(new LineBreak());
            }
        }
    }
}
