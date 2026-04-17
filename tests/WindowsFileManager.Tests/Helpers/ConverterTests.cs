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
    public void BoolToVisibility_ConvertBack_NonVisibility_ShouldReturnFalse()
    {
        var converter = new BoolToVisibilityConverter();

        var result = converter.ConvertBack("not a visibility", typeof(bool), null!, CultureInfo.InvariantCulture);

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

    [Fact]
    public void InverseBoolToVisibility_True_ShouldReturnCollapsed()
    {
        var c = new InverseBoolToVisibilityConverter();
        c.Convert(true, typeof(Visibility), null!, CultureInfo.InvariantCulture).Should().Be(Visibility.Collapsed);
    }

    [Fact]
    public void InverseBoolToVisibility_False_ShouldReturnVisible()
    {
        var c = new InverseBoolToVisibilityConverter();
        c.Convert(false, typeof(Visibility), null!, CultureInfo.InvariantCulture).Should().Be(Visibility.Visible);
    }

    [Fact]
    public void InverseBoolToVisibility_NonBool_ShouldReturnVisible()
    {
        var c = new InverseBoolToVisibilityConverter();
        c.Convert("x", typeof(Visibility), null!, CultureInfo.InvariantCulture).Should().Be(Visibility.Visible);
    }

    [Fact]
    public void InverseBoolToVisibility_ConvertBack_Visible_ShouldReturnFalse()
    {
        var c = new InverseBoolToVisibilityConverter();
        c.ConvertBack(Visibility.Visible, typeof(bool), null!, CultureInfo.InvariantCulture).Should().Be(false);
    }

    [Fact]
    public void InverseBoolToVisibility_ConvertBack_Collapsed_ShouldReturnTrue()
    {
        var c = new InverseBoolToVisibilityConverter();
        c.ConvertBack(Visibility.Collapsed, typeof(bool), null!, CultureInfo.InvariantCulture).Should().Be(true);
    }

    [Fact]
    public void InverseBoolToVisibility_ConvertBack_NonVisibility_ShouldReturnFalse()
    {
        var c = new InverseBoolToVisibilityConverter();
        c.ConvertBack("x", typeof(bool), null!, CultureInfo.InvariantCulture).Should().Be(false);
    }

    [Theory]
    [InlineData(100.0, "20", 80.0)]
    [InlineData(50.0, "100", 0.0)]
    [InlineData(200.0, "50", 150.0)]
    public void Subtract_ValidParam_ShouldSubtract(double width, string param, double expected)
    {
        SubtractConverter.Instance.Convert(width, typeof(double), param, CultureInfo.InvariantCulture).Should().Be(expected);
    }

    [Fact]
    public void Subtract_NonDouble_ShouldReturnValue()
    {
        SubtractConverter.Instance.Convert("not a double", typeof(double), "10", CultureInfo.InvariantCulture).Should().Be("not a double");
    }

    [Fact]
    public void Subtract_InvalidParam_ShouldReturnValue()
    {
        SubtractConverter.Instance.Convert(100.0, typeof(double), "invalid", CultureInfo.InvariantCulture).Should().Be(100.0);
    }

    [Fact]
    public void Subtract_NonStringParam_ShouldReturnValue()
    {
        SubtractConverter.Instance.Convert(100.0, typeof(double), 10, CultureInfo.InvariantCulture).Should().Be(100.0);
    }

    [Fact]
    public void Subtract_ConvertBack_ShouldThrow()
    {
        var act = () => SubtractConverter.Instance.ConvertBack(100.0, typeof(double), "10", CultureInfo.InvariantCulture);
        act.Should().Throw<NotSupportedException>();
    }
}
