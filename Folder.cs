using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using HarfBuzzSharp;

namespace ViSync;

public class Folder
{
    public ObservableCollection<Folder>? SubFolders { get; }
    public string Title { get; }
    public string Path { get; }
    
    private Folder(string title, string path, ObservableCollection<Folder>? subFolders)
    {
        Title = title;
        SubFolders = subFolders;
        Path = path;
    }

    public Folder(string path)
    {
        if (!Directory.Exists(path)) throw new DirectoryNotFoundException();

        var mainDir = new DirectoryInfo(path);

        var madeFolder = MakeFolder(mainDir);

        Title = madeFolder.Title;
        Path = madeFolder.Path;
        SubFolders = madeFolder.SubFolders;
    }

    private static Folder MakeFolder(DirectoryInfo parentFolder)
    {
        var subFolders = new ObservableCollection<Folder>();
        
        foreach (var folder in parentFolder.GetDirectories())
        {
            subFolders.Add(MakeFolder(folder));
        }

        //todo: order by numerical order (look at gallery)
        
        foreach (var file in parentFolder.GetFiles())
        {
            subFolders.Add(new Folder("📄" + file.Name, file.FullName, null));
        }

        return new Folder("📁" + parentFolder.Name, parentFolder.FullName, subFolders);
    }
}