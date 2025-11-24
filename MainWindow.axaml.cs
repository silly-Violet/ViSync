using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Templates;

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

    private string LocalPath { get; set; } = "D:/Music/Main";
    
    public MainWindow()
    {
        InitializeComponent();
        
        SetupLocalPath();
    }

    private void SetupLocalPath()
    {
        LocalPathInputBox.Text = LocalPath;
        ToolTip.SetTip(LocalPathInputBox, LocalPathInputBox.Text);

        LocalFolder = [new Folder(LocalPath)];
    }

}