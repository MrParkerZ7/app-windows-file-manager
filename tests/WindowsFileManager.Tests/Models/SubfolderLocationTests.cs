using FluentAssertions;
using WindowsFileManager.Core.Models;

namespace WindowsFileManager.Tests.Models;

public class SubfolderLocationTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var loc = new SubfolderLocation();

        loc.ParentPath.Should().BeEmpty();
        loc.FullPath.Should().BeEmpty();
    }

    [Fact]
    public void Properties_ShouldRoundTrip()
    {
        var loc = new SubfolderLocation
        {
            ParentPath = @"C:\Projects\app",
            FullPath = @"C:\Projects\app\node_modules",
        };

        loc.ParentPath.Should().Be(@"C:\Projects\app");
        loc.FullPath.Should().Be(@"C:\Projects\app\node_modules");
    }
}
