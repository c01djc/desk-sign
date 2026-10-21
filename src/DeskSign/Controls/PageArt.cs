using System.Globalization;
using System.Windows;
using System.Windows.Media;
using DeskSign.Core;

namespace DeskSign.Controls;

internal static class PageArt
{
    private static readonly Brush PageBrush = Brushes.White;
    private static readonly Brush InkBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A)));
    private static readonly Brush GhostBrush = Freeze(new SolidColorBrush(Color.FromArgb(70, 0x1A, 0x1A, 0x1A)));
    private static readonly Brush GuideBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80)));
    private static readonly Brush FoldBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xC4, 0xB6, 0xA2)));
    private static readonly Brush StrongFoldBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xA6, 0x84, 0x62)));
    private static readonly Pen PagePen = CreatePagePen();

    public static void Draw(
        DrawingContext context,
        SignLayout layout,
        string name,
        string fontName,
        bool showFoldLine,
        bool isPlaceholder,
        double pixelsPerDip,
        double lineWeight,
        bool emphasizeFold,
        Brush? pageBrush = null)
    {
        context.DrawRectangle(pageBrush ?? PageBrush, PagePen, new Rect(0, 0, SignLayout.PageWidthPt, SignLayout.PageHeightPt));

        var guidePen = new Pen(GuideBrush, lineWeight)
        {
            DashStyle = DashStyles.Dash,
            StartLineCap = PenLineCap.Flat,
            EndLineCap = PenLineCap.Flat
        };
        guidePen.Freeze();

        if (showFoldLine)
        {
            var foldPen = new Pen(emphasizeFold ? StrongFoldBrush : FoldBrush, emphasizeFold ? lineWeight * 2.2 : lineWeight)
            {
                DashStyle = DashStyles.Dash
            };
            foldPen.Freeze();
            var foldY = SignLayout.PageHeightPt / 2;
            context.DrawLine(foldPen, new Point(18, foldY), new Point(SignLayout.PageWidthPt - 18, foldY));
        }

        context.DrawLine(
            guidePen,
            new Point(layout.GuideLeftPt, layout.TopGuideYPt),
            new Point(layout.GuideLeftPt + layout.GuideWidthPt, layout.TopGuideYPt));
        context.DrawLine(
            guidePen,
            new Point(layout.GuideLeftPt, layout.BottomGuideYPt),
            new Point(layout.GuideLeftPt + layout.GuideWidthPt, layout.BottomGuideYPt));

        context.PushClip(new RectangleGeometry(new Rect(0, 0, SignLayout.PageWidthPt, SignLayout.PageHeightPt)));
        DrawName(context, layout, name, fontName, layout.TopBoxPt, rotate: true, isPlaceholder, pixelsPerDip);
        DrawName(context, layout, name, fontName, layout.BottomBoxPt, rotate: false, isPlaceholder, pixelsPerDip);
        context.Pop();
    }

    private static void DrawName(
        DrawingContext context,
        SignLayout layout,
        string name,
        string fontName,
        double boxTop,
        bool rotate,
        bool isPlaceholder,
        double pixelsPerDip)
    {
        var text = string.IsNullOrWhiteSpace(name) ? "姓名" : name;
        FormattedText formatted;
        try
        {
            formatted = new FormattedText(
                text,
                CultureInfo.GetCultureInfo("zh-CN"),
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily(fontName), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                layout.FontSizePt,
                isPlaceholder ? GhostBrush : InkBrush,
                pixelsPerDip)
            {
                MaxTextWidth = SignLayout.PageWidthPt,
                TextAlignment = TextAlignment.Center
            };
        }
        catch (ArgumentException)
        {
            return;
        }

        var y = boxTop + (layout.TextBoxHeightPt - formatted.Height) / 2;
        if (rotate)
        {
            context.PushTransform(new RotateTransform(180, SignLayout.PageWidthPt / 2, boxTop + layout.TextBoxHeightPt / 2));
            context.DrawText(formatted, new Point(0, y));
            context.Pop();
        }
        else
        {
            context.DrawText(formatted, new Point(0, y));
        }
    }

    private static T Freeze<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }

    private static Pen CreatePagePen()
    {
        var brush = new SolidColorBrush(Color.FromRgb(0xE4, 0xDB, 0xCE));
        brush.Freeze();
        var pen = new Pen(brush, 1);
        pen.Freeze();
        return pen;
    }
}
