using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using FluentAssertions;

namespace Protender.Tests;

public class TestClass
{
    public string? Testprop { get; set; }
}

public class UiHelperTests
{
    [Fact]
    public void GetValueFromControl_ShouldGiveValidType_WhenGivenValidPropertyInfo()
    {
        var testClass = new TestClass();
        var type = testClass.GetType();
        var test = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Single();

        var result = UiHelper.GetValueFromControl(test, "tester");

        result.Should().BeOfType<string>();
    }

    //[Fact]
    // Fact removed until making a proper MVVM pattern :)
    public void GetControlByType_ShouldThrow_WhenGivenInvalidPropertyType()
    {
        var testClass = new TestClass();
        var type = testClass.GetType();
        var test = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Single();

        var result = UiHelper.GetControlByType(test);

        result.Should().BeOfType<FrameworkElement>();
        result.Should().BeOfType<TextBlock>();
    }
}