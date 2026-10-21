namespace DeskSign.Core;

/// <summary>
/// A4 对折桌签版式。默认字号 150 磅、相距 14 厘米时，与「生成桌签.vbs」一致：
/// 两个姓名框在页面中线相接，姓名在各自区域里居中，辅助线距字形边缘 0.5 厘米。
/// </summary>
public sealed class SignLayout
{
    public const double PointsPerCm = 72.0 / 2.54;
    public const double PageWidthPt = 595.35;
    public const double PageHeightPt = 841.95;
    public const int PageWidthTwips = 11907;
    public const int PageHeightTwips = 16839;
    public const double DefaultFontSizePt = 150;
    public const double DefaultSpacingCm = 14;
    public const double GuideGapCm = 0.5;
    public const double GuideWidthRatio = 0.78;
    public const double MinimumFontSizePt = 36;
    public const double MaximumFontSizePt = 300;

    private SignLayout(
        double fontSizePt,
        double spacingCm,
        double textBoxHeightPt,
        double topBoxPt,
        double bottomBoxPt,
        double topGuideYPt,
        double bottomGuideYPt,
        double guideLeftPt,
        double guideWidthPt)
    {
        FontSizePt = fontSizePt;
        SpacingCm = spacingCm;
        TextBoxHeightPt = textBoxHeightPt;
        TopBoxPt = topBoxPt;
        BottomBoxPt = bottomBoxPt;
        TopGuideYPt = topGuideYPt;
        BottomGuideYPt = bottomGuideYPt;
        GuideLeftPt = guideLeftPt;
        GuideWidthPt = guideWidthPt;
    }

    public double FontSizePt { get; }
    public double SpacingCm { get; }
    public double TextBoxHeightPt { get; }
    public double TopBoxPt { get; }
    public double BottomBoxPt { get; }
    public double TopGuideYPt { get; }
    public double BottomGuideYPt { get; }
    public double GuideLeftPt { get; }
    public double GuideWidthPt { get; }

    public double TopCenterPt => TopBoxPt + TextBoxHeightPt / 2;
    public double BottomCenterPt => BottomBoxPt + TextBoxHeightPt / 2;
    public bool GuidesFitOnPage => TopGuideYPt >= 0 && BottomGuideYPt <= PageHeightPt;

    public static double MaxSpacingCm { get; } = Math.Floor(PageHeightPt / PointsPerCm / 2 * 10) / 10;

    public static double MinimumSpacingCm(double fontSizePt)
    {
        var fontCm = fontSizePt * 2.54 / 72.0;
        var min = Math.Max(4.0, fontCm);
        var snapped = Math.Ceiling(min * 10) / 10.0;
        return Math.Min(snapped, MaxSpacingCm);
    }

    public static double MaximumGuideOffsetCm(double fontSizePt, double spacingCm)
    {
        fontSizePt = Math.Clamp(fontSizePt, MinimumFontSizePt, MaximumFontSizePt);
        spacingCm = Math.Clamp(spacingCm, MinimumSpacingCm(fontSizePt), MaxSpacingCm);
        var boxHeight = spacingCm * PointsPerCm;
        var topCenter = (PageHeightPt - boxHeight) / 2;
        var roomPt = topCenter - fontSizePt / 2;
        return Math.Max(0, Math.Floor(roomPt / PointsPerCm * 10) / 10);
    }

    public static SignLayout Create(double fontSizePt, double spacingCm, double guideOffsetCm = GuideGapCm)
    {
        fontSizePt = Math.Clamp(fontSizePt, MinimumFontSizePt, MaximumFontSizePt);
        var minSpacing = MinimumSpacingCm(fontSizePt);
        spacingCm = Math.Clamp(spacingCm, minSpacing, MaxSpacingCm);
        guideOffsetCm = Math.Clamp(guideOffsetCm, 0, MaximumGuideOffsetCm(fontSizePt, spacingCm));

        var boxHeight = spacingCm * PointsPerCm;
        var top = (PageHeightPt - 2 * boxHeight) / 2;
        var bottom = PageHeightPt / 2;
        var guideGap = guideOffsetCm * PointsPerCm;
        var guideWidth = PageWidthPt * GuideWidthRatio;
        var guideLeft = (PageWidthPt - guideWidth) / 2;
        var topGuide = top + boxHeight / 2 - fontSizePt / 2 - guideGap;
        var bottomGuide = bottom + boxHeight / 2 + fontSizePt / 2 + guideGap;

        return new SignLayout(
            fontSizePt,
            spacingCm,
            boxHeight,
            top,
            bottom,
            topGuide,
            bottomGuide,
            guideLeft,
            guideWidth);
    }
}
