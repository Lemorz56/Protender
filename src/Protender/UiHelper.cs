using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Color = System.Drawing.Color;

namespace Protender;

public static class UiHelper
{
    public enum ConnectionStatus
    {
        Closed = 0,
        Connected = 1,
        Disconnected = 2,
        Reconnecting = 3
    }

    public static FrameworkElement GetControlByType(PropertyInfo prop)
    {
        FrameworkElement inputControl;

        if (prop.PropertyType == typeof(string) || prop.PropertyType == typeof(int) ||
            prop.PropertyType == typeof(double) || prop.PropertyType == typeof(decimal))
        {
            inputControl = new TextBox
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5)
            };

            inputControl.SetBinding(TextBox.TextProperty, new Binding(prop.Name)
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
        }
        else if (prop.PropertyType == typeof(bool))
        {
            inputControl = new CheckBox
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5)
            };

            inputControl.SetBinding(ToggleButton.IsCheckedProperty, new Binding(prop.Name)
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
        }
        else
        {
            inputControl = new TextBlock
            {
                Text = "Not supported",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5)
            };
        }

        return inputControl;
    }

    public static object GetValueFromControl(PropertyInfo propertyInfo, string valueFromControl)
    {
        bool success;
        if (propertyInfo.PropertyType == typeof(string)) return valueFromControl;

        if (propertyInfo.PropertyType == typeof(bool))
        {
            success = bool.TryParse(valueFromControl, out var value);
            if (success) return value;
        }
        else if (propertyInfo.PropertyType == typeof(int))
        {
            success = int.TryParse(valueFromControl, out var value);
            if (success) return value;
        }
        else if (propertyInfo.PropertyType == typeof(double))
        {
            success = double.TryParse(valueFromControl, out var value);
            if (success) return value;
        }
        else if (propertyInfo.PropertyType == typeof(float))
        {
            success = float.TryParse(valueFromControl, out var value);
            if (success) return value;
        }
        else if (propertyInfo.PropertyType == typeof(DateTime))
        {
            success = DateTime.TryParse(valueFromControl, out var value);
            if (success) return value;
        }
        else
        {
            MessageBox.Show($"Type {propertyInfo.PropertyType} is not implemented.");
            // todo: throw or return null?
            throw new NotImplementedException($"Type {propertyInfo.PropertyType} is not implemented.");
        }

        if (success == false) MessageBox.Show($"Could not parse {valueFromControl} to type {propertyInfo}");

        throw new NotImplementedException($"Type {propertyInfo.PropertyType} is not implemented.");
    }
}