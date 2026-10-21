using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DeskSign.Core;
using DeskSign.ViewModels;
using Microsoft.Win32;

namespace DeskSign;

public partial class MainWindow : Window
{
    private readonly MainViewModel _model = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _model;
    }

    private void Import_Click(object sender, RoutedEventArgs e) => Import();

    private void Import()
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择姓名名单（TXT，每行一个姓名）",
            Filter = "文本文件|*.txt|所有文件|*.*",
            CheckFileExists = true
        };
        if (Directory.Exists(_model.LastDirectory))
            dialog.InitialDirectory = _model.LastDirectory;

        if (dialog.ShowDialog(this) == true)
            LoadFile(dialog.FileName);
    }

    private void Generate_Click(object sender, RoutedEventArgs e) => Generate();

    private void Generate()
    {
        var names = _model.Names;
        if (names.Count == 0)
        {
            MessageBox.Show(this, "名单中没有可用姓名。", ProductInfo.Name, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "保存桌签文档",
            Filter = "Word 文档|*.docx",
            FileName = "桌签.docx",
            AddExtension = true,
            OverwritePrompt = true
        };
        if (Directory.Exists(_model.LastDirectory))
            dialog.InitialDirectory = _model.LastDirectory;

        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            DocxDeskSignWriter.Write(dialog.FileName, names, FontNames.ForDocument(_model.FontName), _model.Layout);
            _model.RememberDirectory(dialog.FileName);
            MessageBox.Show(
                this,
                $"桌签已生成，共 {names.Count} 页：{Environment.NewLine}{dialog.FileName}",
                ProductInfo.Name,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, ProductInfo.Name, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e) => _model.NamesText = "";

    private void FlatMode_Checked(object sender, RoutedEventArgs e) => _model.IsFoldDemo = false;

    private void FoldMode_Checked(object sender, RoutedEventArgs e) => _model.IsFoldDemo = true;

    private void Previous_Click(object sender, RoutedEventArgs e) => _model.Previous();

    private void Next_Click(object sender, RoutedEventArgs e) => _model.Next();

    private void CommitOnEnter(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not TextBox box)
            return;
        box.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        e.Handled = true;
    }

    private void Preview_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Delta > 0)
            _model.Previous();
        else
            _model.Next();
        e.Handled = true;
    }

    private void Window_PreviewDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        var textFile = files.FirstOrDefault(file => file.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));
        if (textFile is not null)
            LoadFile(textFile);
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Handled)
            return;

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.O)
        {
            Import();
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
        {
            Generate();
            e.Handled = true;
            return;
        }

        if (e.OriginalSource is TextBox)
            return;

        if (e.Key == Key.Left)
        {
            _model.Previous();
            e.Handled = true;
        }
        else if (e.Key == Key.Right)
        {
            _model.Next();
            e.Handled = true;
        }
    }

    private void LoadFile(string path)
    {
        try
        {
            _model.NamesText = NameListReader.ReadFile(path);
            _model.RememberDirectory(path);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "无法读取名单。\n" + ex.Message, ProductInfo.Name, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
