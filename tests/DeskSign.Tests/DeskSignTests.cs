using DeskSign.Core;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;

namespace DeskSign.Tests;

public class FontNameTests
{
    [Fact]
    public void MapsCommonEnglishFontNamesToWordNames()
    {
        Assert.Equal("宋体", FontNames.ForDocument("SimSun"));
        Assert.Equal("微软雅黑", FontNames.ForDocument("Microsoft YaHei"));
        Assert.Equal("楷体", FontNames.ForDocument("楷体"));
    }
}

public class LayoutTests
{
    [Fact]
    public void DefaultLayoutMatchesOriginalScript()
    {
        var layout = SignLayout.Create(150, 14);

        Assert.InRange(layout.TopBoxPt, 24.12, 24.13);
        Assert.InRange(layout.BottomBoxPt, 420.97, 420.98);
        Assert.InRange(layout.TextBoxHeightPt, 396.85, 396.86);
        Assert.InRange(layout.TopGuideYPt, 133.37, 133.39);
        Assert.InRange(layout.BottomGuideYPt, 708.56, 708.59);
        Assert.InRange(layout.GuideWidthPt, 464.37, 464.38);
        Assert.Equal(14, (layout.BottomCenterPt - layout.TopCenterPt) / SignLayout.PointsPerCm, 2);
    }

    [Fact]
    public void SpacingMovesNameCentersAndKeepsTheFoldInTheMiddle()
    {
        var layout = SignLayout.Create(150, 10);

        Assert.Equal(SignLayout.PageHeightPt / 2, layout.BottomBoxPt, 3);
        Assert.Equal(10, (layout.BottomCenterPt - layout.TopCenterPt) / SignLayout.PointsPerCm, 2);
        Assert.Equal(layout.TopCenterPt - 75 - 0.5 * SignLayout.PointsPerCm, layout.TopGuideYPt, 3);
    }

    [Fact]
    public void SpacingCannotShrinkBelowTheFont()
    {
        var layout = SignLayout.Create(150, 2);
        Assert.True(layout.TextBoxHeightPt >= 150);
    }

    [Fact]
    public void GuideOffsetMovesBothDashedLinesAwayFromTheNames()
    {
        var layout = SignLayout.Create(150, 14, 1.5);
        var gap = 1.5 * SignLayout.PointsPerCm;
        Assert.Equal(layout.TopCenterPt - 75 - gap, layout.TopGuideYPt, 3);
        Assert.Equal(layout.BottomCenterPt + 75 + gap, layout.BottomGuideYPt, 3);
    }
}

public class NameListTests
{
    [Fact]
    public void ParseSkipsBlankLinesAndTrims()
    {
        var names = NameListReader.Parse(" 张三 \r\n\r\n李四\n  \n王五");
        Assert.Equal(["张三", "李四", "王五"], names);
    }

    [Fact]
    public void ReadFileAcceptsUtf8AndGbk()
    {
        var directory = Path.Combine(Path.GetTempPath(), "desksign-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var utf8 = Path.Combine(directory, "utf8.txt");
            File.WriteAllText(utf8, "张三\n李四");
            Assert.Equal("张三\n李四", NameListReader.ReadFile(utf8).Replace("\r\n", "\n"));

            var gbk = Path.Combine(directory, "gbk.txt");
            File.WriteAllBytes(gbk, [0xD5, 0xC5, 0xC8, 0xFD, 0x0A, 0xC0, 0xEE, 0xCB, 0xC4]);
            Assert.Equal("张三\n李四", NameListReader.ReadFile(gbk));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}

public class DocxWriterTests
{
    [Fact]
    public void WriteBuildsOneFoldedPagePerName()
    {
        var path = Path.Combine(Path.GetTempPath(), "desksign-tests", Guid.NewGuid().ToString("N") + ".docx");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try
        {
            var layout = SignLayout.Create(150, 14);
            DocxDeskSignWriter.Write(path, ["张三", "A&B<C>"], "宋体", layout);

            using var doc = WordprocessingDocument.Open(path, false);
            var xml = doc.MainDocumentPart!.Document!.OuterXml;
            Assert.Contains("张三", xml);
            Assert.Contains("A&amp;B&lt;C&gt;", xml);
            Assert.Contains("宋体", xml);
            Assert.Contains("w:val=\"300\"", xml);
            Assert.Contains("rot=\"10800000\"", xml);
            Assert.Contains("808080", xml);
            Assert.Contains($"w:w=\"{SignLayout.PageWidthTwips}\"", xml);
            Assert.Equal(2, Count(xml, "rot=\"10800000\""));
            Assert.Equal(2, Count(xml, "<w:sectPr>"));
            Assert.Equal(ProductInfo.Developer, doc.PackageProperties.Creator);

            var errors = new OpenXmlValidator()
                .Validate(doc)
                .Where(error => error.ErrorType == ValidationErrorType.Schema)
                .Select(error => error.Description)
                .ToList();
            Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static int Count(string text, string token)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }

        return count;
    }
}
