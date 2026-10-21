using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using DeskSign.Core;
using DeskSign.Services;

namespace DeskSign.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly bool _loading;
    private string _namesText = "";
    private string _fontName = "宋体";
    private double _fontSizePt = SignLayout.DefaultFontSizePt;
    private double _spacingCm = SignLayout.DefaultSpacingCm;
    private double _guideOffsetCm = SignLayout.GuideGapCm;
    private bool _showFoldLine = true;
    private bool _isFoldDemo;
    private int _previewIndex;
    private string? _lastDirectory;
    private IReadOnlyList<string> _names = [];

    public MainViewModel()
    {
        _loading = true;
        Fonts = LoadFonts();
        var settings = SettingsStore.Load();
        _fontName = MatchFont(settings.FontName) ?? DefaultFont();
        _fontSizePt = Math.Clamp(settings.FontSizePt, SignLayout.MinimumFontSizePt, SignLayout.MaximumFontSizePt);
        _spacingCm = Math.Clamp(settings.SpacingCm, SignLayout.MinimumSpacingCm(_fontSizePt), SignLayout.MaxSpacingCm);
        _guideOffsetCm = Math.Clamp(settings.GuideOffsetCm, 0, SignLayout.MaximumGuideOffsetCm(_fontSizePt, _spacingCm));
        _showFoldLine = settings.ShowFoldLine;
        _lastDirectory = settings.LastDirectory;
        _loading = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<string> Fonts { get; }

    public string NamesText
    {
        get => _namesText;
        set
        {
            if (_namesText == value)
                return;
            _namesText = value;
            _names = NameListReader.Parse(value);
            if (_previewIndex >= _names.Count)
                _previewIndex = Math.Max(0, _names.Count - 1);
            OnPropertyChanged();
            NotifyNames();
        }
    }

    public string FontName
    {
        get => _fontName;
        set
        {
            var next = string.IsNullOrWhiteSpace(value) ? DefaultFont() : value.Trim();
            if (_fontName == next)
                return;
            _fontName = next;
            OnPropertyChanged();
            NotifyLayout();
            Save();
        }
    }

    public double FontSizePt
    {
        get => _fontSizePt;
        set
        {
            var next = Math.Clamp(Math.Round(value), SignLayout.MinimumFontSizePt, SignLayout.MaximumFontSizePt);
            if (Math.Abs(next - _fontSizePt) < 0.001)
                return;
            _fontSizePt = next;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SpacingMinimumCm));
            var min = SpacingMinimumCm;
            if (_spacingCm < min)
            {
                _spacingCm = min;
                OnPropertyChanged(nameof(SpacingCm));
            }
            ClampGuideOffset();
            NotifyLayout();
            Save();
        }
    }

    public double SpacingCm
    {
        get => _spacingCm;
        set
        {
            var next = Math.Round(Math.Clamp(value, SpacingMinimumCm, SpacingMaximumCm), 1);
            if (Math.Abs(next - _spacingCm) < 0.001)
                return;
            _spacingCm = next;
            OnPropertyChanged();
            ClampGuideOffset();
            NotifyLayout();
            Save();
        }
    }

    public double GuideOffsetCm
    {
        get => _guideOffsetCm;
        set
        {
            var next = Math.Round(Math.Clamp(value, 0, GuideOffsetMaximumCm), 1);
            if (Math.Abs(next - _guideOffsetCm) < 0.001)
                return;
            _guideOffsetCm = next;
            OnPropertyChanged();
            NotifyLayout();
            Save();
        }
    }

    public bool IsFoldDemo
    {
        get => _isFoldDemo;
        set
        {
            if (_isFoldDemo == value)
                return;
            _isFoldDemo = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PreviewHint));
        }
    }

    public bool ShowFoldLine
    {
        get => _showFoldLine;
        set
        {
            if (_showFoldLine == value)
                return;
            _showFoldLine = value;
            OnPropertyChanged();
            Save();
        }
    }

    public int PreviewIndex
    {
        get => _previewIndex;
        set
        {
            var max = Math.Max(0, _names.Count - 1);
            var next = Math.Clamp(value, 0, max);
            if (_previewIndex == next)
                return;
            _previewIndex = next;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PreviewName));
            OnPropertyChanged(nameof(PageLabel));
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
        }
    }

    public string? LastDirectory
    {
        get => _lastDirectory;
        set
        {
            if (_lastDirectory == value)
                return;
            _lastDirectory = value;
            OnPropertyChanged();
            Save();
        }
    }

    public IReadOnlyList<string> Names => _names;
    public bool HasNames => _names.Count > 0;
    public bool IsPlaceholder => _names.Count == 0;
    public string PreviewName => _names.Count == 0 ? "姓名" : _names[_previewIndex];
    public string NameCountText => _names.Count == 0 ? "还没有姓名" : $"共 {_names.Count} 人";
    public string PageLabel => _names.Count == 0 ? "预览" : $"{_previewIndex + 1} / {_names.Count}";
    public bool CanGoPrevious => _previewIndex > 0;
    public bool CanGoNext => _previewIndex < _names.Count - 1;
    public bool CanGenerate => _names.Count > 0;
    public string GenerateButtonText => _names.Count == 0 ? "生成桌签" : $"生成桌签（{_names.Count} 页）";
    public string GenerateToolTip => _names.Count == 0 ? "请先输入姓名" : "保存为 Word 文档（Ctrl+S）";
    public string PreviewHint => IsFoldDemo
        ? "立体演示：A4 比夹子大，沿灰色虚线折好再放进去。拖动可以看另一面。"
        : "上半部分旋转 180°。沿正中对折后，正反两面都能读到姓名。灰色虚线会打印出来，浅色对折线只在预览里。";
    public double SpacingMinimumCm => SignLayout.MinimumSpacingCm(FontSizePt);
    public double SpacingMaximumCm => SignLayout.MaxSpacingCm;
    public double GuideOffsetMaximumCm => SignLayout.MaximumGuideOffsetCm(FontSizePt, SpacingCm);
    public SignLayout Layout => SignLayout.Create(FontSizePt, SpacingCm, GuideOffsetCm);

    public string LayoutWarning
    {
        get
        {
            if (!Layout.GuidesFitOnPage)
                return "灰色虚线超出了纸张范围，可以减小虚线距。";

            if (!HasNames)
                return "";

            foreach (var name in _names)
            {
                if (MeasureWidthPt(name) > SignLayout.PageWidthPt * 0.98)
                    return "有姓名在当前字号下可能超出页面宽度。";
            }

            return "";
        }
    }

    public bool HasWarning => LayoutWarning.Length > 0;

    public void Previous() => PreviewIndex--;

    public void Next() => PreviewIndex++;

    public void RememberDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;
        var directory = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            LastDirectory = directory;
    }

    private void NotifyNames()
    {
        OnPropertyChanged(nameof(Names));
        OnPropertyChanged(nameof(HasNames));
        OnPropertyChanged(nameof(IsPlaceholder));
        OnPropertyChanged(nameof(PreviewName));
        OnPropertyChanged(nameof(NameCountText));
        OnPropertyChanged(nameof(PageLabel));
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CanGenerate));
        OnPropertyChanged(nameof(GenerateButtonText));
        OnPropertyChanged(nameof(GenerateToolTip));
        NotifyLayout();
    }

    private void ClampGuideOffset()
    {
        OnPropertyChanged(nameof(GuideOffsetMaximumCm));
        var max = GuideOffsetMaximumCm;
        if (_guideOffsetCm > max)
        {
            _guideOffsetCm = max;
            OnPropertyChanged(nameof(GuideOffsetCm));
        }
    }

    private void NotifyLayout()
    {
        OnPropertyChanged(nameof(Layout));
        OnPropertyChanged(nameof(LayoutWarning));
        OnPropertyChanged(nameof(HasWarning));
    }

    private void Save()
    {
        if (_loading)
            return;

        try
        {
            SettingsStore.Save(new UserSettings
            {
                FontName = FontName,
                FontSizePt = FontSizePt,
                SpacingCm = SpacingCm,
                GuideOffsetCm = GuideOffsetCm,
                ShowFoldLine = ShowFoldLine,
                LastDirectory = LastDirectory
            });
        }
        catch (IOException)
        {
        }
    }

    private double MeasureWidthPt(string text)
    {
        try
        {
            var formatted = new FormattedText(
                text,
                CultureInfo.GetCultureInfo("zh-CN"),
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily(FontName), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                FontSizePt * 96.0 / 72.0,
                Brushes.Black,
                1.0);
            return formatted.Width * 72.0 / 96.0;
        }
        catch (ArgumentException)
        {
            return 0;
        }
    }

    private string? MatchFont(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;
        return Fonts.FirstOrDefault(font => font.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private string DefaultFont()
    {
        foreach (var name in new[] { "宋体", "SimSun", "新宋体", "NSimSun", "微软雅黑", "Microsoft YaHei" })
        {
            if (Fonts.Contains(name, StringComparer.OrdinalIgnoreCase))
                return Fonts.First(font => font.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        return Fonts.Count > 0 ? Fonts[0] : "宋体";
    }

    private static IReadOnlyList<string> LoadFonts()
    {
        var preferred = new[] { "宋体", "黑体", "微软雅黑", "楷体", "仿宋", "等线", "SimSun", "SimHei", "Microsoft YaHei", "KaiTi", "FangSong" };
        var names = System.Windows.Media.Fonts.SystemFontFamilies
            .Select(DisplayName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var pinned = preferred
            .Select(name => names.FirstOrDefault(font => font.Equals(name, StringComparison.OrdinalIgnoreCase)))
            .Where(name => name is not null)
            .Cast<string>()
            .ToList();
        var rest = names
            .Where(name => !pinned.Contains(name, StringComparer.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.CurrentCulture);

        return pinned.Concat(rest).ToList();
    }

    private static string DisplayName(FontFamily family)
    {
        var source = family.Source;
        var hash = source.LastIndexOf('#');
        var name = hash >= 0 && hash < source.Length - 1 ? source[(hash + 1)..] : source;
        return name switch
        {
            "SimSun" => "宋体",
            "NSimSun" => "新宋体",
            "SimHei" => "黑体",
            "Microsoft YaHei" => "微软雅黑",
            "KaiTi" => "楷体",
            "FangSong" => "仿宋",
            "DengXian" => "等线",
            "LiSu" => "隶书",
            "YouYuan" => "幼圆",
            _ => name
        };
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
