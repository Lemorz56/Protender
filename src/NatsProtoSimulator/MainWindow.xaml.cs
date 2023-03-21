using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using Testing;

namespace NatsProtoSimulator;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private TestMessage _myClass;

    public MainWindow()
    {
        InitializeComponent();

        CreateUI();
    }

    private void CreateUI()
    {
        // Create the grid
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

        // Get the properties of the class
        var properties = typeof(TestMessage).GetProperties()
            .Where(p => p.Name != "Parser" && p.Name != "Descriptor");

        // Create a TextBlock and TextBox for each property
        for (var i = 0; i < properties.Count(); i++)
        {
            var property = properties.ElementAt(i);

            // Create a TextBlock to display the name and type of the property
            var propertyNameTextBlock = new TextBlock();
            propertyNameTextBlock.Inlines.Add(new Run(property.Name));
            propertyNameTextBlock.Inlines.Add(new Run($" ({property.PropertyType.Name})")
                { FontWeight = FontWeights.Bold });
            propertyNameTextBlock.Margin = new Thickness(5);

            // Create a TextBox or CheckBox to edit the value of the property
            FrameworkElement propertyValueControl = null;
            if (property.PropertyType == typeof(bool))
            {
                var checkBox = new CheckBox();
                checkBox.SetBinding(ToggleButton.IsCheckedProperty, new Binding(property.Name));
                propertyValueControl = checkBox;
            }
            else
            {
                var textBox = new TextBox();
                textBox.SetBinding(TextBox.TextProperty, new Binding(property.Name));
                propertyValueControl = textBox;
            }

            propertyValueControl.Margin = new Thickness(5);

            // Add the controls to the grid
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            Grid.SetColumn(propertyNameTextBlock, 0);
            Grid.SetRow(propertyNameTextBlock, i);
            Grid.SetColumn(propertyValueControl, 1);
            Grid.SetRow(propertyValueControl, i);
            grid.Children.Add(propertyNameTextBlock);
            grid.Children.Add(propertyValueControl);
        }

        // Create a button to write a message to the console
        var consoleButton = new Button();
        consoleButton.Content = "Write to Console";
        consoleButton.Margin = new Thickness(5);
        consoleButton.Click += (sender, args) =>
        {
            // Create an instance of the class and set its properties based on the values in the TextBoxes and CheckBoxes
            var instance = new TestMessage();
            foreach (var property in properties)
                if (property.PropertyType == typeof(bool))
                {
                    var checkBox = grid.Children.OfType<CheckBox>()
                        .FirstOrDefault(c => Grid.GetRow(c) == properties.ToList().IndexOf(property));
                    property.SetValue(instance, checkBox.IsChecked);
                }
                else
                {
                    var textBox = grid.Children.OfType<TextBox>()
                        .FirstOrDefault(c => Grid.GetRow(c) == properties.ToList().IndexOf(property));
                    var value = Convert.ChangeType(textBox.Text, property.PropertyType);
                    property.SetValue(instance, value);
                }

            // Write the instance to the console
            Console.WriteLine(instance.ToString());
        };

        // Add the button to the grid
        grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
        Grid.SetColumn(consoleButton, 0);
        Grid.SetRow(consoleButton, properties.Count());
        Grid.SetColumnSpan(consoleButton, 2);
        grid.Children.Add(consoleButton);

        // Add the grid to the window
        Content = grid;
    }

    private void ConsoleButton_Click(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("Button clicked!");
    }
}