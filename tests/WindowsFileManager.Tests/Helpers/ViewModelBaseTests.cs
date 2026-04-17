using FluentAssertions;
using WindowsFileManager.ViewModels;

namespace WindowsFileManager.Tests.Helpers;

public class ViewModelBaseTests
{
    [Fact]
    public void SetProperty_NewValue_ShouldRaiseAndReturnTrue()
    {
        var vm = new Dummy();
        var changes = new List<string>();
        vm.PropertyChanged += (_, e) => changes.Add(e.PropertyName!);

        vm.Value = 5;

        vm.Value.Should().Be(5);
        changes.Should().ContainSingle(p => p == nameof(Dummy.Value));
    }

    [Fact]
    public void SetProperty_SameValue_ShouldNotRaiseOrReturnFalse()
    {
        var vm = new Dummy { Value = 5 };
        var changes = new List<string>();
        vm.PropertyChanged += (_, e) => changes.Add(e.PropertyName!);

        vm.Value = 5;

        changes.Should().BeEmpty();
    }

    [Fact]
    public void OnPropertyChanged_ExplicitName_ShouldRaise()
    {
        var vm = new Dummy();
        var changes = new List<string>();
        vm.PropertyChanged += (_, e) => changes.Add(e.PropertyName!);

        vm.Raise("Something");

        changes.Should().ContainSingle(p => p == "Something");
    }

    [Fact]
    public void PropertyChanged_NoSubscribers_ShouldNotThrow()
    {
        var vm = new Dummy();
        var act = () => vm.Value = 42;
        act.Should().NotThrow();
    }

    private class Dummy : ViewModelBase
    {
        private int _value;

        public int Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public void Raise(string name) => OnPropertyChanged(name);
    }
}
