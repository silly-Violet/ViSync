using System.Collections.ObjectModel;
using System.IO;

namespace ViSync;

public class ViStorageItem
{
    public ObservableCollection<ViStorageItem>? SubItems { get; }
    public string Title { get; }
    private string Path { get; }
    public int Type { get; }
    //public string? Hash { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="title"></param>
    /// <param name="path"></param>
    /// <param name="type">0 == Folder, 1 == File</param>
    /// <param name="subItems"></param>
    ///// <param name="hash"></param>
    private ViStorageItem(string title, string path, int type, ObservableCollection<ViStorageItem>? subItems)//, string? hash = null
    {
        Title = title;
        SubItems = subItems;
        Path = path;
        Type = type;
        //Hash = hash;
    }

    public ViStorageItem(string path)
    {
        if (!Directory.Exists(path)) throw new DirectoryNotFoundException();

        var mainDir = new DirectoryInfo(path);

        var madeFolder = MakeFolder(mainDir, path);

        Title = madeFolder.Title;
        Path = madeFolder.Path;
        SubItems = madeFolder.SubItems;
    }

    private static ViStorageItem MakeFolder(DirectoryInfo parentFolder, string rootFolder)
    {
        var subItems = new ObservableCollection<ViStorageItem>();
        
        foreach (var folder in parentFolder.GetDirectories())
        {
            subItems.Add(MakeFolder(folder, rootFolder));
        }

        
        foreach (var file in parentFolder.GetFiles())
        {
            subItems.Add(new ViStorageItem("📄" + file.Name, file.FullName[rootFolder.Length..].Replace('\\', '/'), 1,null)); //, GetHashString(file)));
        }

        return new ViStorageItem("📁" + parentFolder.Name, parentFolder.FullName[rootFolder.Length..].Replace('\\', '/'), 0, subItems);
    }
    
    
    /*private static string GetHashString(FileInfo file)
    {
        using var fileStream = file.OpenRead();

        var byteBuffer = new byte[fileStream.Length];

        fileStream.ReadExactly(byteBuffer);

        var hashBytes = MD5.HashData(byteBuffer);

        var output = "";
        
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var hashByte in hashBytes)
        {
            output += hashByte.ToString("X2");
        }

        return output;
    }*/
}