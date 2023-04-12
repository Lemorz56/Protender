using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Google.Protobuf;
using Microsoft.Win32;
using NATS.Client;
using static Protender.UiHelper;
using Serilog;

namespace Protender;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Dictionary<string, Type> _classes = new();
    private string _selectedFile = "";
    private IConnection? _connection;

    private NatsPublisher? _natsPublisher;

    public MainWindow()
    {
        InitializeComponent();

        ClassComboBox.IsEnabled = false;
        NatsSubjectText.Visibility = Visibility.Hidden;
        NatsSubjectBox.Visibility = Visibility.Hidden;
        MessageCountText.Visibility = Visibility.Hidden;
        MessageCountBox.Visibility = Visibility.Hidden;
    }

    private void ClassComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var comboItem = ClassComboBox.SelectedItem;
        var ss = comboItem.ToString();
        if (ss == null) return;
        var selectedType = _classes[ss];
        CreateUi(selectedType);
    }


    // todo: this REALLY needs to be refactored and moved
    private void CreateUi(Type? type)
    {
        if (type == null) return;

        // Since IMessage has these properties we want to skip them
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
                Text = $"{prop.Name} ({prop.PropertyType.Name})",
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5)
            };

            var inputControl = GetControlByType(prop);

            Grid.SetRow(textBlock, i);
            Grid.SetColumn(textBlock, 0);
            Grid.SetRow(inputControl, i);
            Grid.SetColumn(inputControl, 1);

            grid.Children.Add(textBlock);
            grid.Children.Add(inputControl);
        }

        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

        var consoleButton = new Button
        {
            Content = "Publish to NATS",
            Margin = new Thickness(5),
            Padding = new Thickness(10),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        consoleButton.Click += (_, _) => { NatsPublishClick(type); };

        Grid.SetRow(consoleButton, properties.Length);
        Grid.SetColumnSpan(consoleButton, 2);

        grid.Children.Add(consoleButton);
    }

    private void FileSelector_OnClick(object sender, RoutedEventArgs e)
    {
        var openFileDlg = new OpenFileDialog
        {
            Filter = "DLL files (*.dll)|*.dll",
            Title = "Select a DLL file"
        };

        if (openFileDlg.ShowDialog() == true)
        {
            _selectedFile = openFileDlg.FileName;
            Log.Debug("Selected file {file} to open", _selectedFile);

            try
            {
                var splitList = _selectedFile.Split("\\");
                var splitTest = splitList.TakeLast(1).SingleOrDefault();
                if (splitTest == null)
                    throw new NullReferenceException(nameof(splitTest));

                var sanitizedAssemblyName = splitTest.Replace(".dll", "");
                _classes = AssemblyUtils.LoadClasses(sanitizedAssemblyName);
                if (_classes.Count > 0)
                {
                    ClassComboBox.IsEnabled = true;
                    ClassComboBox.ItemsSource = _classes.Keys;
                    return;
                }

                MessageBox.Show($"Could not load {_selectedFile} correctly, no classes found.");
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Something went wrong when loading classes from {file}", _selectedFile);
            }
        }
    }

    private void NatsConnectButton_OnClick(object sender, RoutedEventArgs e)
    {
        NatsConnectButton.IsEnabled = false;
        ConnectToNats(NatsUrlBox.Text);
    }

    private void ConnectToNats(string connectionUrl)
    {
        if (string.IsNullOrWhiteSpace(connectionUrl))
        {
            MessageBox.Show("Please enter a valid URL!");
            return;
        }

        var opts = SetupOptions(connectionUrl);

        var connectionFactory = new ConnectionFactory();
        IConnection connection;
        try
        {
            connection = connectionFactory.CreateConnection(opts);
        }
        catch (NATSConnectionException e)
        {
            Log.Debug($"Failed to create a nats connection to {opts.Url}.");
            MessageBox.Show(e.Message);
            NatsConnectButton.IsEnabled = true;
            return;
        }
        catch (Exception e)
        {
            MessageBox.Show($"FATAL: {e.Message}");
            Log.Error(e, "Could not create a NATS connection.");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }

        if (connection.IsClosed())
        {
            SetNatsStatus(ConnectionStatus.Closed);
            return;
        }

        _connection = connection;
        _natsPublisher = new NatsPublisher(_connection);

        ConnStatus.Text = "CONNECTED";
        ConnStatus.Foreground = new SolidColorBrush(Colors.Green);
        // todo:
        NatsSubjectText.Visibility = Visibility.Visible;
        NatsSubjectBox.Visibility = Visibility.Visible;
        MessageCountText.Visibility = Visibility.Visible;
        MessageCountBox.Visibility = Visibility.Visible;
    }

    private Options SetupOptions(string connectionUrl)
    {
        var opts = ConnectionFactory.GetDefaultOptions();
        opts.Url = connectionUrl;

        opts.Name = $"{Environment.MachineName}-NatsProto";
        opts.AllowReconnect = true;
        opts.DisconnectedEventHandler += async (sender, args) =>
        {
            await Dispatcher.InvokeAsync(() => SetNatsStatus(ConnectionStatus.Disconnected));
        };

        opts.ClosedEventHandler += async (sender, args) =>
        {
            await Dispatcher.InvokeAsync(() => SetNatsStatus(ConnectionStatus.Closed));
        };

        opts.ReconnectedEventHandler += async (sender, args) =>
        {
            await Dispatcher.InvokeAsync(() => SetNatsStatus(ConnectionStatus.Reconnecting));
        };

        return opts;
    }

    private void SetNatsStatus(ConnectionStatus status)
    {
        switch (status)
        {
            case ConnectionStatus.Closed:
                ConnStatus.Text = "CLOSED";
                ConnStatus.Foreground = new SolidColorBrush(Colors.Red);
                NatsConnectButton.IsEnabled = true;

                NatsSubjectText.Visibility = Visibility.Hidden;
                NatsSubjectBox.Visibility = Visibility.Hidden;
                MessageCountText.Visibility = Visibility.Hidden;
                MessageCountBox.Visibility = Visibility.Hidden;
                break;
            case ConnectionStatus.Connected:
                ConnStatus.Text = "CONNECTED";
                ConnStatus.Foreground = new SolidColorBrush(Colors.Green);
                NatsSubjectText.Visibility = Visibility.Visible;
                NatsSubjectBox.Visibility = Visibility.Visible;
                MessageCountText.Visibility = Visibility.Visible;
                MessageCountBox.Visibility = Visibility.Visible;
                break;
            case ConnectionStatus.Disconnected:
                ConnStatus.Text = "DISCONNECTED";
                ConnStatus.Foreground = new SolidColorBrush(Colors.Orange);
                NatsConnectButton.IsEnabled = true;

                NatsSubjectText.Visibility = Visibility.Hidden;
                NatsSubjectBox.Visibility = Visibility.Hidden;
                MessageCountText.Visibility = Visibility.Hidden;
                MessageCountBox.Visibility = Visibility.Hidden;
                break;
            case ConnectionStatus.Reconnecting:
                ConnStatus.Text = "RECONNECTING...";
                ConnStatus.Foreground = new SolidColorBrush(Colors.Blue);
                break;
            default:
                ConnStatus.Text = "UNKNOWN";
                ConnStatus.Foreground = new SolidColorBrush(Colors.Black);
                NatsConnectButton.IsEnabled = true;

                NatsSubjectText.Visibility = Visibility.Hidden;
                NatsSubjectBox.Visibility = Visibility.Hidden;
                MessageCountText.Visibility = Visibility.Hidden;
                MessageCountBox.Visibility = Visibility.Hidden;
                break;
        }
    }

    private void NatsPublishClick(Type type)
    {
        // Create an instance of the type that was passed to CreateUI
        var instance = Activator.CreateInstance(type);

        // Assign the property values from the UI controls to the instance
        foreach (var child in grid.Children)
        {
            if (child is TextBox textBox && Grid.GetColumn(textBox) == 1)
            {
                var propertyName = grid.Children
                    .OfType<TextBlock>()
                    .First(tb => Grid.GetRow(tb) == Grid.GetRow(textBox) && Grid.GetColumn(tb) == 0)
                    .Text.Split(" ");

                var propertyInfo = type.GetProperty(propertyName[0]);

                if (propertyInfo != null)
                {
                    var value = GetValueFromControl(propertyInfo,
                        textBox.Text);
                    propertyInfo.SetValue(instance, value);
                }
            }

            if (child is CheckBox checkBox && Grid.GetColumn(checkBox) == 1)
            {
                var propertyName = grid.Children
                    .OfType<TextBlock>()
                    .First(tb => Grid.GetRow(tb) == Grid.GetRow(checkBox) && Grid.GetColumn(tb) == 0)
                    .Text.Split(" ");

                var propertyInfo = type.GetProperty(propertyName[0]);

                if (propertyInfo != null)
                    propertyInfo.SetValue(instance, checkBox.IsChecked);
            }
        }

        // todo: unsafe
        var messageInstance = (IMessage)instance!;
        if (messageInstance != null)
        {
            if (_natsPublisher == null) throw new NullReferenceException(nameof(_natsPublisher));

            _natsPublisher.PublishMessage(NatsSubjectBox.Text, messageInstance.ToByteArray(),
                int.Parse(MessageCountBox.Text));

            MessageBox.Show(
                $"Published {MessageCountBox.Text} of {instance?.GetType().Name} to {NatsSubjectBox.Text}");
        }
        else
        {
            MessageBox.Show("Type is not of IMessage...");
        }
    }

    private void MessageCountBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !TextIsNumeric(e.Text);
    }

    private static bool TextIsNumeric(string input)
    {
        return input.All(c => char.IsDigit(c) || char.IsControl(c));
    }
}