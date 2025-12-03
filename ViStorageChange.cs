using System.IO;

namespace ViSync;

public class ViStorageChange(string fromPath, string toPath, int type, bool isFolder)
{
    public const int AddType = 0;
    //public const int RemoveType = 1;
    
    private string FromPath { get; } = fromPath.Replace('\\', '/');
    public string ToPath { get; } = toPath.Replace('\\', '/');

    private int Type { get; } = type;
    private bool IsFolder { get; } = isFolder;

    //public bool IsEnabled { get; set; } = true;

    public void ApplyChange()
    {
        //if (!IsEnabled) return;
        
        switch (Type)
        {
            case AddType:
                if (IsFolder)
                {
                    Directory.CreateDirectory(ToPath);
                    CopyDirectory(new DirectoryInfo(FromPath), new DirectoryInfo(ToPath));
                }
                else
                {
                    Directory.CreateDirectory(ToPath[..ToPath.LastIndexOf('/')]);
                    File.Copy(FromPath, ToPath, false);
                }
                break;
            //case RemoveType:
                //break;
        }
    }

    //https://stackoverflow.com/questions/58744
    private static void CopyDirectory(DirectoryInfo source, DirectoryInfo target)
    {
        foreach (var dir in source.GetDirectories())
        {
            CopyDirectory(dir, target.CreateSubdirectory(dir.Name));
        }
        foreach (var file in source.GetFiles())
        {
            Directory.CreateDirectory(target.FullName);
            file.CopyTo(Path.Combine(target.FullName, file.Name));
        }
            
    }
}