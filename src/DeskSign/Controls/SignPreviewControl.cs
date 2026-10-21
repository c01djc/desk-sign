using System.Globalization;
using System.Windows;
using System.Windows.Media;
using DeskSign.Core;

namespace DeskSign.Controls;

public sealed class SignPreviewControl : FrameworkElement
{
    public static readonly DependencyProperty NameTextProperty = Register(nameof(NameText), "");
    public static readonly DependencyProperty FontNameProperty = Register(nameof(FontName), "宋体");
    public static readonly DependencyProperty FontSizePtProperty = Register(nameof(FontSizePt), SignLayout.DefaultFontSizePt);
    public static readonly DependencyProperty SpacingCmProperty = Register(nameof(SpacingCm), SignLayout.DefaultSpacingCm);
    public static readonly DependencyProperty GuideOffsetCmProperty = Register(nameof(GuideOffsetCm), SignLayout.GuideGapCm);
    public static readonly DependencyProperty ShowFoldLineProperty = Register(nameof(ShowFoldLine), true);
    public static readonly DependencyProperty IsPlaceholderProperty = Register(nameof(IsPlaceholder), false);

    private static readonly Brush ShadowBrush = Freeze(new SolidColorBrush(Color.FromArgb(28, 30, 40, 36)));

    public SignPreviewControl()
    {
        TextOptions.SetTextFormattingMode(this, TextFormattingMode.Ideal);
        SnapsToDevicePixels = true;
    }

    public string NameText
    {
        get => (string)GetValue(NameTextProperty);
        set => SetValue(NameTextProperty, value);
    }

    public string FontName
    {
        get => (string)GetValue(FontNameProperty);
        set => SetValue(FontNameProperty, value);
    }

    public double FontSizePt
    {
        get => (double)GetValue(FontSizePtProperty);
        set => SetValue(FontSizePtProperty, value);
    }

    public double SpacingCm
    {
        get => (double)GetValue(SpacingCmProperty);
        set => SetValue(SpacingCmProperty, value);
    }

    public double GuideOffsetCm
    {
        get => (double)GetValue(GuideOffsetCmProperty);
        set => SetValue(GuideOffsetCmProperty, value);
    }

    public bool ShowFoldLine
    {
        get => (bool)GetValue(ShowFoldLineProperty);
        set => SetValue(ShowFoldLineProperty, value);
    }

    public bool IsPlaceholder
    {
        get => (bool)GetValue(IsPlaceholderProperty);
        set => SetValue(IsPlaceholderProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 480 : Math.Max(0, availableSize.Width);
        var height = double.IsInfinity(availableSize.Height) ? 640 : Math.Max(0, availableSize.Height);
        return new Size(width, height);
    }

    protected override void OnRender(DrawingContext context)
    {
        if (ActualWidth < 8 || ActualHeight < 8)
            return;

        var layout = SignLayout.Create(FontSizePt, SpacingCm, GuideOffsetCm);
        var margin = 28.0;
        var scale = Math.Min(
            (ActualWidth - margin * 2) / SignLayout.PageWidthPt,
            (ActualHeight - margin * 2) / SignLayout.PageHeightPt);
        if (scale <= 0)
            return;

        var originX = (ActualWidth - SignLayout.PageWidthPt * scale) / 2;
        var originY = (ActualHeight - SignLayout.PageHeightPt * scale) / 2;

        context.PushTransform(new TranslateTransform(originX, originY));
        context.PushTransform(new ScaleTransform(scale, scale));

        context.DrawRectangle(ShadowBrush, null, new Rect(5, 7, SignLayout.PageWidthPt, SignLayout.PageHeightPt));
        PageArt.Draw(
            context,
            layout,
            NameText,
            FontName,
            ShowFoldLine,
            IsPlaceholder,
            VisualTreeHelper.GetDpi(this).PixelsPerDip,
            Math.Max(1.0, 1.15 / scale),
            emphasizeFold: false);
        context.Pop();
        context.Pop();
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        InvalidateVisual();
    }

    private static DependencyProperty Register<T>(string name, T defaultValue) =>
        DependencyProperty.Register(
            name,
            typeof(T),
            typeof(SignPreviewControl),
            new FrameworkPropertyMetadata(defaultValue, FrameworkPropertyMetadataOptions.AffectsRender));

    private static T Freeze<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
