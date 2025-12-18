using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DialogHostAvalonia;

namespace ViSync;

public partial class MainWindow : Window
{
    private ObservableCollection<ViStorageItem>? _localFolder;
    public ObservableCollection<ViStorageItem> LocalFolder
    {
        get => _localFolder ?? [];

        set
        {
            _localFolder = value;
            LocalFolderTree.ItemsSource = _localFolder;
        }
    }

    private string? LocalPath { get; set; }
    
    
    private ObservableCollection<ViStorageItem>? _awayFolder;
    public ObservableCollection<ViStorageItem> AwayFolder
    {
        get => _awayFolder ?? [];

        set
        {
            _awayFolder = value;
            AwayFolderTree.ItemsSource = _awayFolder;
        }
    }

    private string? AwayPath { get; set; }
    
    private List<ViStorageChange>? IncomingChanges { get; set; }
    private List<ViStorageChange>? OutgoingChanges { get; set; }

    private readonly string _previousPathSavePath = Directory.GetCurrentDirectory().Replace('\\', '/') + "/previouspaths.txt";
    
    private readonly ProgressBar _fileCopyProgressBar = new()
    {
        ProgressTextFormat = "Files/Folders Copied: {0}/{3} ({1:0}%)",
        Foreground = new SolidColorBrush(new Color(0xFF, 0xAC, 0x58, 0xDA)),
        Minimum = 0,
        ShowProgressText = true,
        Height = 30,
        Margin = new Thickness(5, 0, 5, 5),
        Width = 400
    };
    
    public MainWindow()
    {
        InitializeComponent();

        if (File.Exists(_previousPathSavePath))
            LoadFromPreviousPaths();
        else
        {
            using var fileStream = new FileStream(_previousPathSavePath, FileMode.Create);
            using var writer = new StreamWriter(fileStream);
            writer.Write("\n");
        }
        
        SetupPath();
        SetupPath(false);
    }

    private void SetupPath(bool isLocal = true)
    {
        if (isLocal)
        {
            if (LocalPath == null) return;
        
            LocalPathInputBox.Text = LocalPath;
            ToolTip.SetTip(LocalPathInputBox, LocalPathInputBox.Text);
        
            LocalFolder = [new ViStorageItem(LocalPath)];
        }
        else
        {
            if (AwayPath == null) return;

            AwayPathInputBox.Text = AwayPath;
            ToolTip.SetTip(AwayPathInputBox, AwayPathInputBox.Text);

            AwayFolder = [new ViStorageItem(AwayPath)];
        }
    }

    private void SavePreviousPaths()
    {
        using var fileStream = new FileStream(_previousPathSavePath, FileMode.Create);
        using var writer = new StreamWriter(fileStream);
        
        if (LocalPath != null)
            writer.Write(LocalPath);
        
        writer.Write("\n");
        
        if (AwayPath != null)
            writer.Write(AwayPath);
    }

    private void LoadFromPreviousPaths()
    {
        using var fileStream = new FileStream(_previousPathSavePath, FileMode.Open);
        using var reader = new StreamReader(fileStream);

        var readLocal = reader.ReadLine();
        var readAway = reader.ReadLine();

        if (!string.IsNullOrEmpty(readLocal)) LocalPath = readLocal;
        if (!string.IsNullOrEmpty(readAway)) AwayPath = readAway;
    }
    
    private void SetupPath(string path, bool isLocal = true)
    {
        if (isLocal)
            LocalPath = path;
        else
            AwayPath = path;
        
        SetupPath(isLocal);
    }

    private void FillChangePanels()
    {
        if (IncomingChanges == null || OutgoingChanges == null) return;

        OutgoingChangesPanel.Children.Clear();
        
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var change in OutgoingChanges)
        {
            var label = new Label
            {
                Content = change.ToPath[AwayPath!.Length..]
            };
            
            OutgoingChangesPanel.Children.Add(label);
        }

        IncomingChangesPanel.Children.Clear();
        
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var change in IncomingChanges)
        {
            var label = new Label
            {
                Content = change.ToPath[LocalPath!.Length..]
            };
            
            IncomingChangesPanel.Children.Add(label);
        }
    }
    
    private async Task<string?> FolderPrompt(string title)
    {
        var folder = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            Title = title,
            AllowMultiple = false
        });

        if (folder.Count != 1) return null;

        // ReSharper disable once InvertIf
        if (!folder[0].Path.IsAbsoluteUri) // root of drive or other invalid places
        {
            ShowDialogMessage("This path is not supported", false,true);
            return null;
        }
        
        return folder[0].Path.AbsolutePath.Replace("%20", " ");
    }
    
    private static List<ViStorageChange> FindChanges(string localPath, string localPathRoot, string awayPathRoot)
    {
        var output = new List<ViStorageChange>();

        var localInfo = new DirectoryInfo(localPath);
        
        foreach (var folder in localInfo.GetDirectories())
        {
            var folderInAway = awayPathRoot + folder.FullName[localPathRoot.Length..];
            
            if (Directory.Exists(folderInAway))
            {
                output.AddRange(FindChanges(folder.FullName, localPathRoot, awayPathRoot));
            }
            else
            {
                output.Add(new ViStorageChange(folder.FullName, folderInAway, ViStorageChange.AddType, true));
                output.AddRange(FindChanges(folder.FullName, localPathRoot, awayPathRoot));
            }
        }

        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var file in localInfo.GetFiles())
        {
            var fileInAway = awayPathRoot + file.FullName[localPathRoot.Length..];
            
            if (!File.Exists(fileInAway))
            {
                output.Add(new ViStorageChange(file.FullName, fileInAway, ViStorageChange.AddType, false));
            }
        }

        return output;
    }
    
    private void ShowDialogMessage(string message, bool buttonCenterAligned = false, bool labelCenterAligned = false,
        bool confirmMessage = false, bool protectEnter = false, EventHandler<RoutedEventArgs>? confirmButtonClick = null)
    {
        var label = new Label()
        {
            Content = message,
            Margin = new Thickness(5, 5, 5, 10),
            HorizontalAlignment = labelCenterAligned ? HorizontalAlignment.Center : HorizontalAlignment.Left
        };
            
        var okButton = new Button()
        {
            Content = confirmMessage ? "Cancel" : "Close",
            Classes = { "Main" },
            HotKey = new KeyGesture(confirmMessage ? Key.Escape : Key.Enter),
            Width = 100,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        okButton.Click += (_, _) => DialogHost.IsOpen = false;

        var buttonStack = new StackPanel()
        {
            Orientation = Orientation.Horizontal,
            Children = { okButton },
            HorizontalAlignment = buttonCenterAligned ? HorizontalAlignment.Center : HorizontalAlignment.Right
        };
        
        //Make it options
        if (confirmMessage)
        {
            var confirmButton = new Button()
            {
                Content = "Confirm",
                Classes = { "Main" },
                HotKey = protectEnter ? null : new KeyGesture(Key.Enter),
                Width = 100
            };
            if (confirmButtonClick != null) confirmButton.Click += confirmButtonClick;
            
            buttonStack.Children.Add(confirmButton);
        }
        
        
        var mainStack = new StackPanel()
        {
            Orientation = Orientation.Vertical,
            Children = { label, buttonStack },
            Margin = new Thickness(25, 25, 25, 10)
        };
        
        var dialogContent = new Border()
        {
            BorderBrush = new SolidColorBrush(new Color(255, 221, 221, 221)),
            BorderThickness = new Thickness(1),
            Child = mainStack,
            Margin = new Thickness(-5)
        };
            
        DialogHost.Show(dialogContent);
    }

    private async Task ApplyChanges()
    {
        await Task.Run(() =>
        {
            if (IncomingChanges != null)
            {
                foreach (var change in from inChange in IncomingChanges where inChange.IsFolder select inChange)
                {
                    change.ApplyChange();
                    Dispatcher.UIThread.Invoke(ProgressFileCopyBar);
                }

                foreach (var change in from inChange in IncomingChanges where !inChange.IsFolder select inChange)
                {
                    change.ApplyChange();
                    Dispatcher.UIThread.Invoke(ProgressFileCopyBar);
                }
            }

            if (OutgoingChanges != null)
            {
                foreach (var change in from outChange in OutgoingChanges where outChange.IsFolder select outChange)
                {
                    change.ApplyChange();
                    Dispatcher.UIThread.Invoke(ProgressFileCopyBar);
                }

                foreach (var change in from outChange in OutgoingChanges where !outChange.IsFolder select outChange)
                {
                    change.ApplyChange();
                    Dispatcher.UIThread.Invoke(ProgressFileCopyBar);
                }
            }
        });
    }

    private void ProgressFileCopyBar()
    {
        _fileCopyProgressBar.Value++;
    }
    
    private async void BrowseClick(object? sender, RoutedEventArgs e)
    {
        var isLocal = ((Button)sender!).CommandParameter!.ToString() == "Local";
        
        var folderPath = await FolderPrompt($"Choose {(isLocal ? "Local" : "Away")} Folder");
        
        if (!Directory.Exists(folderPath)) return;
        
        SetupPath(folderPath, isLocal);
        SavePreviousPaths();
    }

    private void ScanClick(object? sender, RoutedEventArgs e)
    {
        if (!Directory.Exists(LocalPath) || !Directory.Exists(AwayPath)) return;

        IncomingChanges = FindChanges(AwayPath, AwayPath, LocalPath);
        OutgoingChanges = FindChanges(LocalPath, LocalPath, AwayPath);
        
        // ReSharper disable once MergeIntoPattern
        if (IncomingChanges != null || OutgoingChanges != null)
        {
            ApplyChangesButton.IsEnabled = true;
        }
        
        FillChangePanels();
    }

    private async void ApplyChangesClick(object? sender, RoutedEventArgs e)
    {
        var label = new Label
        {
            Content = "Copying files...\nPlease don't close the program",
            Margin = new Thickness(5, 5, 5, 10),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        
        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Children = { label, _fileCopyProgressBar }
        };

        _fileCopyProgressBar.Value = 0;
        _fileCopyProgressBar.Maximum = (IncomingChanges?.Count ?? 0) + (OutgoingChanges?.Count ?? 0);
        
        DialogHost.Show(stack);
        
        await ApplyChanges();
        DialogHost.IsOpen = false;
        
        SetupPath();
        SetupPath(false);
            
        ScanClick(null, new RoutedEventArgs());
        ApplyChangesButton.IsEnabled = false;
        
        ShowDialogMessage("Files Copied!", true, true);
    }
}