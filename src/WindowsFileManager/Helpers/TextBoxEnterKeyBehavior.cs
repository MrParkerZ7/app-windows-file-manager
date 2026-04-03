using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WindowsFileManager.Helpers;

/// <summary>
/// Attached behavior that executes a command when Enter is pressed in a TextBox.
/// </summary>
[ExcludeFromCodeCoverage]
public static class TextBoxEnterKeyBehavior
{
    /// <summary>
    /// Identifies the Command attached property.
    /// </summary>
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached(
            "Command",
            typeof(ICommand),
            typeof(TextBoxEnterKeyBehavior),
            new PropertyMetadata(null, OnCommandChanged));

    /// <summary>
    /// Gets the command for the specified TextBox.
    /// </summary>
    public static ICommand? GetCommand(DependencyObject obj) =>
        (ICommand?)obj.GetValue(CommandProperty);

    /// <summary>
    /// Sets the command for the specified TextBox.
    /// </summary>
    public static void SetCommand(DependencyObject obj, ICommand? value) =>
        obj.SetValue(CommandProperty, value);

    private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox)
        {
            textBox.KeyDown -= OnKeyDown;
            if (e.NewValue is ICommand)
            {
                textBox.KeyDown += OnKeyDown;
            }
        }
    }

    private static void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox textBox)
        {
            // Update binding before executing command
            var binding = textBox.GetBindingExpression(TextBox.TextProperty);
            binding?.UpdateSource();

            var command = GetCommand(textBox);
            if (command?.CanExecute(null) == true)
            {
                command.Execute(null);
            }
        }
    }
}
