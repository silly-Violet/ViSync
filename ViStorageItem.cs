using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using HarfBuzzSharp;

namespace ViSync;

public class ViStorageItem
{
    public ObservableCollection<ViStorageItem>? SubFolders { get; }
    public string Title { get; }
    public string Path { get; }
    
    public int Type { get; }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="title"></param>
    /// <param name="path"></param>
    /// <param name="type">0 == Folder, 1 == File</param>
    /// <param name="subFolders"></param>
    private ViStorageItem(string title, string path, int type, ObservableCollection<ViStorageItem>? subFolders)
    {
        Title = title;
        SubFolders = subFolders;
        Path = path;
        Type = type;
    }

    public ViStorageItem(string path)
    {
        if (!Directory.Exists(path)) throw new DirectoryNotFoundException();

        var mainDir = new DirectoryInfo(path);

        var madeFolder = MakeFolder(mainDir);

        Title = madeFolder.Title;
        Path = madeFolder.Path;
        SubFolders = madeFolder.SubFolders;
    }

    private static ViStorageItem MakeFolder(DirectoryInfo parentFolder)
    {
        var subFolders = new ObservableCollection<ViStorageItem>();
        
        foreach (var folder in parentFolder.GetDirectories())
        {
            subFolders.Add(MakeFolder(folder));
        }

        //todo: order by numerical order (look at gallery)
        
        foreach (var file in parentFolder.GetFiles())
        {
            subFolders.Add(new ViStorageItem("📄" + file.Name, file.FullName, 1,null));
        }

        return new ViStorageItem("📁" + parentFolder.Name, parentFolder.FullName, 0, subFolders);
    }
}