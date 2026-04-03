using System.Globalization;
using System.Windows;
using FluentAssertions;
using WindowsFileManager.Helpers;

namespace WindowsFileManager.Tests.Helpers;

public class ConverterTests
{
    [Fact]
    public void BoolToVisibility_True_ShouldReturnVisible()
    {
        var converter = new BoolToVisibilityConverter();

        var result = converter.Convert(true, typeof(Visibility), null!, CultureInfo.InvariantCulture);

        result.Should().Be(Visibility.Visible);
    }

    [Fact]
    public void BoolToVisibility_False_ShouldReturnCollapsed()
    {
        var converter = new BoolToVisibilityConverter();

        var result = converter.Convert(false, typeof(Visibility), null!, CultureInfo.InvariantCulture);

        result.Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void BoolToVisibility_NonBool_ShouldReturnCollapsed()
    {
        var converter = new BoolToVisibilityConverter();

        var result = converter.Convert("not a bool", typeof(Visibility), null!, CultureInfo.InvariantCulture);

        result.Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void BoolToVisibility_ConvertBack_Visible_ShouldReturnTrue()
    {
        var converter = new BoolToVisibilityConverter();

        var result = converter.ConvertBack(Visibility.Visible, typeof(bool), null!, CultureInfo.InvariantCulture);

        result.Should().Be(true);
    }

    [Fact]
    public void BoolToVisibility_ConvertBack_Collapsed_ShouldReturnFalse()
    {
        var converter = new BoolToVisibilityConverter();

        var result = converter.ConvertBack(Visibility.Collapsed, typeof(bool), null!, CultureInfo.InvariantCulture);

        result.Should().Be(false);
    }

    [Fact]
    public void InverseBool_True_ShouldReturnFalse()
    {
        var converter = new InverseBoolConverter();

        var result = converter.Convert(true, typeof(bool), null!, CultureInfo.InvariantCulture);

        result.Should().Be(false);
    }

    [Fact]
    public void InverseBool_False_ShouldReturnTrue()
    {
        var converter = new InverseBoolConverter();

        var result = converter.Convert(false, typeof(bool), null!, CultureInfo.InvariantCulture);

        result.Should().Be(true);
    }

    [Fact]
    public void InverseBool_NonBool_ShouldReturnTrue()
    {
        var converter = new InverseBoolConverter();

        var result = converter.Convert("not a bool", typeof(bool), null!, CultureInfo.InvariantCulture);

        result.Should().Be(true);
    }

    [Fact]
    public void InverseBool_ConvertBack_True_ShouldReturnFalse()
    {
        var converter = new InverseBoolConverter();

        var result = converter.ConvertBack(true, typeof(bool), null!, CultureInfo.InvariantCulture);

        result.Should().Be(false);
    }

    [Fact]
    public void InverseBool_ConvertBack_False_ShouldReturnTrue()
    {
        var converter = new InverseBoolConverter();

        var result = converter.ConvertBack(false, typeof(bool), null!, CultureInfo.InvariantCulture);

        result.Should().Be(true);
    }

    [Fact]
    public void InverseBool_ConvertBack_NonBool_ShouldReturnFalse()
    {
        var converter = new InverseBoolConverter();

        var result = converter.ConvertBack("not a bool", typeof(bool), null!, CultureInfo.InvariantCulture);

        result.Should().Be(false);
    }
}
