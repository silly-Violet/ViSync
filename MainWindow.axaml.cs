using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace ViSync;

public partial class MainWindow : Window
{
    private ObservableCollection<Folder>? _localFolder;
    public ObservableCollection<Folder> LocalFolder
    {
        get => _localFolder ?? [];

        set
        {
            _localFolder = value;
            LocalFolderTree.ItemsSource = _localFolder;
        }
    }

    private string? LocalPath { get; set; }
    
    //todo: support mobile somehow?
    
    public MainWindow()
    {
        InitializeComponent();
        
        SetupLocalPath();
    }

    private void SetupLocalPath()
    {
        if (LocalPath == null) return;
        
        LocalPathInputBox.Text = LocalPath;
        ToolTip.SetTip(LocalPathInputBox, LocalPathInputBox.Text);
        
        LocalFolder = [new Folder(LocalPath)];
    }
    
    private void SetupLocalPath(string path)
    {
        LocalPath = path;
        
        SetupLocalPath();
    }

    private async Task<string?> FolderPrompt(string title)
    {
        var folder = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            Title = title,
            AllowMultiple = false
        });

        if (folder.Count != 1) return null;

        if (!folder[0].Path.IsAbsoluteUri) // root of drive
            return null; //todo: error message
        
        return folder[0].Path.AbsolutePath.Replace("%20", " ");
    }
    
    private async void BrowseClick(object? sender, RoutedEventArgs e)
    {
        var folderPath = await FolderPrompt("Choose Local Folder");

        //todo: debug
        
        if (!Directory.Exists(folderPath)) return;
        
        SetupLocalPath(folderPath);
    }
}