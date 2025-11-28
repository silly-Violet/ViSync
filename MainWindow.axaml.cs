using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

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
    
    //todo: support mobile somehow?
    
    public MainWindow()
    {
        InitializeComponent();
        
        SetupPath();
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
    
    private void SetupPath(string path, bool isLocal = true)
    {
        if (isLocal)
            LocalPath = path;
        else
            AwayPath = path;
        
        SetupPath(isLocal);
    }

    private async Task<string?> FolderPrompt(string title)
    {
        var folder = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            Title = title,
            AllowMultiple = false
        });

        if (folder.Count != 1) return null;

        if (!folder[0].Path.IsAbsoluteUri) // root of drive or other invalid places
            return null; //todo: error message
        
        return folder[0].Path.AbsolutePath.Replace("%20", " ");
    }
    
    private async void BrowseClick(object? sender, RoutedEventArgs e)
    {
        var isLocal = ((Button)sender!).CommandParameter!.ToString() == "Local";
        
        var folderPath = await FolderPrompt($"Choose {(isLocal ? "Local" : "Away")} Folder");
        
        if (!Directory.Exists(folderPath)) return;
        
        SetupPath(folderPath, isLocal);
    }
}