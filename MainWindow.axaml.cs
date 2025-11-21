using Avalonia.Controls;

namespace ViSync;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        LocalPathInputBox.Text = "D:/example/folder/with/files";
        ToolTip.SetTip(LocalPathInputBox, LocalPathInputBox.Text);
    }
}