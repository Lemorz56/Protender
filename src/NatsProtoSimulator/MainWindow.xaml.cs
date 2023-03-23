using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using Testing;

namespace NatsProtoSimulator;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Dictionary<string, Type> _classes = new();

    public MainWindow()
    {
        InitializeComponent();
        LoadClasses("NatsProtoSimulatorProtos");
    }

    private void LoadClasses(string assemblyName)
    {
        var assemblyWorking = FindAssemblyInReferences(assemblyName);

        if (assemblyWorking != null)
            foreach (var type in assemblyWorking.GetTypes().Where(t => t.IsClass && !t.IsAbstract))
                _classes.Add(type.Name, type);

        classComboBox.ItemsSource = _classes.Keys;
    }

    private void classComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var comboItem = classComboBox.SelectedItem;
        var ss = comboItem.ToString();
        if (ss == null) return;
        var selectedType = _classes[ss];
        CreateUI(selectedType);
    }

    //private void ConsoleButton_Click(object sender, RoutedEventArgs e)
    //{
    //    var className = classComboBox.SelectedItem as string;
    //    if (className == null) return;

    //    var selectedType = _classes[className];
    //    var instance = Activator.CreateInstance(selectedType);

    //    foreach (var child in grid.Children)
    //        if (child is TextBlock textBlock && Grid.GetColumn(textBlock) == 0)
    //        {
    //            var oldName = textBlock.Text;

    //            var result = Regex.Replace(textBlock.Text, @"\(.*?\)", string.Empty);
    //            var propertyName = result.Replace("(", "").Replace(")", "");
    //            var trimmedPropName = propertyName.Trim();

    //            var propertyInfo = selectedType.GetProperty(trimmedPropName);
    //            if (propertyInfo != null)
    //            {
    //                var value = GetValueFromChildControl(propertyInfo, textBlock);
    //                propertyInfo.SetValue(instance, value);
    //            }
    //        }

    //    Console.WriteLine(instance);
    //}

    private object GetValueFromChildControl(PropertyInfo propertyInfo, FrameworkElement parent)
    {
        var propertyName = propertyInfo.Name;
        var childControl = parent.FindName(propertyName) as FrameworkElement;
        childControl = parent;

        if (childControl == null)
            throw new ArgumentException(
                $"Control with name {propertyName} not found in the logical tree of the parent control.");

        if (propertyInfo.PropertyType == typeof(string))
        {
            return ((TextBox)childControl).Text;
        }
        else if (propertyInfo.PropertyType == typeof(bool))
        {
            return ((CheckBox)childControl).IsChecked.GetValueOrDefault();
        }
        else if (propertyInfo.PropertyType == typeof(int))
        {
            int.TryParse(((TextBox)childControl).Text, out var value);
            return value;
        }
        else if (propertyInfo.PropertyType == typeof(double))
        {
            double.TryParse(((TextBox)childControl).Text, out var value);
            return value;
        }
        else if (propertyInfo.PropertyType == typeof(float))
        {
            float.TryParse(((TextBox)childControl).Text, out var value);
            return value;
        }
        else if (propertyInfo.PropertyType == typeof(DateTime))
        {
            DateTime.TryParse(((TextBox)childControl).Text, out var value);
            return value;
        }

        throw new NotImplementedException($"Type {propertyInfo.PropertyType} is not implemented.");
    }


    private void CreateUI(Type type)
    {
        if (type == null) return;

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && !p.Name.Equals("Parser") && !p.Name.Equals("Descriptor"))
            .ToArray();

        grid.Children.Clear();
        grid.RowDefinitions.Clear();
        grid.ColumnDefinitions.Clear();

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        for (var i = 0; i < properties.Length; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            var prop = properties[i];

            var textBlock = new TextBlock
            {
                Text = $"{prop.Name}", //todo: ({prop.PropertyType.Name})
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5)
            };

            var binding = new Binding(prop.Name)
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };

            var textBox = new TextBox
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5)
            };

            textBox.SetBinding(TextBox.TextProperty, binding);

            Grid.SetRow(textBlock, i);
            Grid.SetColumn(textBlock, 0);
            Grid.SetRow(textBox, i);
            Grid.SetColumn(textBox, 1);

            grid.Children.Add(textBlock);
            grid.Children.Add(textBox);
        }

        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

        var consoleButton = new Button
        {
            Content = "Log to Console",
            Margin = new Thickness(5),
            Padding = new Thickness(10),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        consoleButton.Click += (sender, args) =>
        {
            // Create an instance of the type that was passed to CreateUI
            var instance = Activator.CreateInstance(type);

            // Assign the property values from the UI controls to the instance
            foreach (var child in grid.Children)
                if (child is TextBlock textBlock && Grid.GetColumn(textBlock) == 0)
                {
                    var propertyName = textBlock.Text.Replace(":", "");
                    var propertyInfo = type.GetProperty(propertyName);
                    if (propertyInfo != null)
                    {
                        var value = GetValueFromChildControl(propertyInfo, child as FrameworkElement);
                        propertyInfo.SetValue(instance, value);
                    }
                }

            // Write the instance to the console
            Console.WriteLine(instance);
        };

        //consoleButton.Click += (sender, args) =>
        //{
        //    var instance = Activator.CreateInstance(type);
        //    foreach (var prop in properties)
        //        prop.SetValue(instance, Convert.ChangeType(prop.GetValue(instance), prop.PropertyType));
        //    Console.WriteLine(instance);
        //    MessageBox.Show(instance.ToString());
        //};

        Grid.SetRow(consoleButton, properties.Length);
        Grid.SetColumnSpan(consoleButton, 2);

        grid.Children.Add(consoleButton);
    }

    public static Assembly? FindAssemblyInReferences(string prefixName)
    {
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
        var loadedPaths = loadedAssemblies.Select(a => a.Location).ToArray();

        var referencedPaths = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");
        var toLoad = referencedPaths.Where(r => !loadedPaths.Contains(r, StringComparer.InvariantCultureIgnoreCase))
            .Where(x => x.Contains(prefixName))
            .ToList();

        toLoad.ForEach(path => loadedAssemblies.Add(AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(path))));

        return loadedAssemblies.FirstOrDefault(x => x.FullName != null && x.FullName.Contains(prefixName));
    }
}