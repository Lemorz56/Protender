using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Testing;

namespace NatsProtoSimulator;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        //DataContextChanged += SearchView_DataContextChanged;
        //DataContext = new TestMessage();

        // Create an instance of MyClass
        var myObject = new TestMessage();
        var testObj = new TestMessage
        {
            PageNumber = 1,
            Query = "geh",
            ResultPerPage = 1
        };

        // Get the type of MyClass
        var objectType = myObject.GetType();

        // Get the properties of MyClass
        var properties = objectType.GetProperties();

        var index = 0;
        // Iterate through the properties and create a TextBox for each property
        foreach (var property in properties)
            if (property.Name != "Parser" && property.Name != "Descriptor")
            {
                var colDef = new ColumnDefinition { Width = GridLength.Auto };
                myGrid.ColumnDefinitions.Add(colDef);

                var rowDef = new RowDefinition { Height = GridLength.Auto };
                myGrid.RowDefinitions.Add(rowDef);

                var textBox = new TextBox
                {
                    Name = property.Name
                };
                var textBlock = new TextBlock
                {
                    Name = property.Name,
                    Text = property.Name
                };

                var binding = new Binding(property.Name)
                {
                    Source = myObject
                };

                textBox.SetBinding(TextBox.TextProperty, binding);

                myGrid.Children.Add(textBox);
                myGrid.Children.Add(textBlock);

                Grid.SetRow(textBox, index++);
                Grid.SetColumn(textBox, index++);

                Grid.SetRow(textBlock, index);
                Grid.SetColumn(textBlock, index);
                index++;
            }
    }

    //private void SetProperty(object target, string propertyName, object value)
    //{
    //    var property = target.GetType().GetProperty(propertyName) ??
    //                   throw new NullReferenceException(nameof(propertyName));
    //    property.SetValue(target, value, null);
    //}

    private void SearchView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue != null)
        {
            var genericType = e.NewValue.GetType();
            Console.WriteLine(genericType);
            //check the DataContext was set to a SearchViewModel<T>
            //if (genericType.GetType() == typeof(TestMessage))
            //{
            //...and create a TextBox for each property of the type T
            //var type = genericType.GetGenericArguments()[0];
            var properties = genericType.GetProperties();
            foreach (var property in properties)
            {
                var textBox = new TextBox();
                var binding = new Binding(property.Name);
                if (!property.CanWrite)
                    binding.Mode = BindingMode.OneWay;
                textBox.SetBinding(TextBox.TextProperty, binding);

                rootPanel.Children.Add(textBox);
            }
            //}
        }
    }
}