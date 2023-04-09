using NATS.Client;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Color = System.Drawing.Color;

namespace NatsProtoSimulator;

public static class UiHelper
{
    public enum ConnectionStatus
    {
        Closed = 0,
        Connected = 1,
        Disconnected = 2,
        Reconnected = 3
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

        if (propertyInfo.PropertyType == typeof(int))
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

        throw new NotImplementedException($"Type {propertyInfo.PropertyType} is not implemented.");
    }

    public static Tuple<string, Color> SetNatsStatus(ConnectionStatus status)
    {
        string text;
        Color color;

        switch (status)
        {
            case ConnectionStatus.Closed:
                text = "CLOSED";
                color = Color.Red;
                break;
            case ConnectionStatus.Connected:
                text = "CONNECTED";
                color = Color.Green;
                break;
            case ConnectionStatus.Disconnected:
                text = "DISCONNECTED";
                color = Color.Orange;
                break;
            case ConnectionStatus.Reconnected:
                text = "RECONNECTING...";
                color = Color.Blue;
                break;
            default:
                text = "UNKNOWN";
                color = Color.Black;
                break;
        }

        return new Tuple<string, Color>(text, color);
    }
}