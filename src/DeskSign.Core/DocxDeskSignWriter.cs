using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DeskSign.Core;

public static class DocxDeskSignWriter
{
    public static void Write(string path, IReadOnlyList<string> names, string fontName, SignLayout layout)
    {
        if (names.Count == 0)
            throw new ArgumentException("名单中没有可用姓名。");

        if (string.IsNullOrWhiteSpace(fontName))
            throw new ArgumentException("请选择字体。");

        if (!path.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            path += ".docx";

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var xml = BuildDocument(names, fontName.Trim(), layout);
        try
        {
            using var word = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            var main = word.AddMainDocumentPart();
            main.Document = new Document { InnerXml = xml };
            main.Document.Save();

            word.PackageProperties.Creator = ProductInfo.Developer;
            word.PackageProperties.LastModifiedBy = ProductInfo.Developer;
            word.PackageProperties.Title = "桌签";
            word.PackageProperties.Subject = $"{ProductInfo.Name} {ProductInfo.Version}";
        }
        catch (IOException ex)
        {
            throw new IOException($"无法写入文件。请确认它没有被 Word 打开。{Environment.NewLine}{path}", ex);
        }
    }

    private static string BuildDocument(IReadOnlyList<string> names, string fontName, SignLayout layout)
    {
        var body = new StringBuilder();
        var id = 1u;
        var halfPoints = ((int)Math.Round(layout.FontSizePt * 2, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture);
        var font = Attr(fontName);

        for (var i = 0; i < names.Count; i++)
        {
            body.Append("<w:p><w:pPr>")
                .Append(ParagraphSpacing())
                .Append("</w:pPr>");
            body.Append(TextBox(id++, "正面", names[i], font, halfPoints, 0, layout.TopBoxPt, SignLayout.PageWidthPt, layout.TextBoxHeightPt, 10_800_000));
            body.Append(TextBox(id++, "反面", names[i], font, halfPoints, 0, layout.BottomBoxPt, SignLayout.PageWidthPt, layout.TextBoxHeightPt, 0));
            body.Append(Guide(id++, "上辅助线", layout.GuideLeftPt, layout.TopGuideYPt, layout.GuideWidthPt));
            body.Append(Guide(id++, "下辅助线", layout.GuideLeftPt, layout.BottomGuideYPt, layout.GuideWidthPt));
            body.Append("</w:p>");

            if (i < names.Count - 1)
            {
                body.Append("<w:p><w:pPr>")
                    .Append(ParagraphSpacing())
                    .Append(SectionProperties("nextPage"))
                    .Append("</w:pPr></w:p>");
            }
        }

        body.Append(SectionProperties(null));

        return "<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" "
            + "xmlns:wp=\"http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing\" "
            + "xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" "
            + "xmlns:wps=\"http://schemas.microsoft.com/office/word/2010/wordprocessingShape\" "
            + "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">"
            + body
            + "</w:body>";
    }

    private static string ParagraphSpacing() =>
        "<w:spacing w:before=\"0\" w:after=\"0\" w:line=\"20\" w:lineRule=\"exact\"/>"
        + "<w:rPr><w:sz w:val=\"2\"/><w:szCs w:val=\"2\"/></w:rPr>";

    private static string SectionProperties(string? type)
    {
        var typeXml = type is null ? "" : $"<w:type w:val=\"{type}\"/>";
        return "<w:sectPr>"
            + typeXml
            + $"<w:pgSz w:w=\"{SignLayout.PageWidthTwips}\" w:h=\"{SignLayout.PageHeightTwips}\" w:orient=\"portrait\" w:code=\"9\"/>"
            + "<w:pgMar w:top=\"0\" w:right=\"0\" w:bottom=\"0\" w:left=\"0\" w:header=\"0\" w:footer=\"0\" w:gutter=\"0\"/>"
            + "<w:cols w:space=\"0\"/>"
            + "<w:docGrid w:linePitch=\"360\"/>"
            + "</w:sectPr>";
    }

    private static string TextBox(
        uint id,
        string shapeName,
        string name,
        string font,
        string halfPoints,
        double xPt,
        double yPt,
        double widthPt,
        double heightPt,
        int rotation)
    {
        var x = Emu(xPt);
        var y = Emu(yPt);
        var width = Emu(widthPt);
        var height = Emu(heightPt);
        var rot = rotation == 0 ? "" : $" rot=\"{rotation.ToString(CultureInfo.InvariantCulture)}\"";

        return "<w:r><w:drawing>"
            + $"<wp:anchor distT=\"0\" distB=\"0\" distL=\"0\" distR=\"0\" simplePos=\"0\" relativeHeight=\"{250000000 + id}\" behindDoc=\"0\" locked=\"0\" layoutInCell=\"1\" allowOverlap=\"1\">"
            + "<wp:simplePos x=\"0\" y=\"0\"/>"
            + $"<wp:positionH relativeFrom=\"page\"><wp:posOffset>{x}</wp:posOffset></wp:positionH>"
            + $"<wp:positionV relativeFrom=\"page\"><wp:posOffset>{y}</wp:posOffset></wp:positionV>"
            + $"<wp:extent cx=\"{width}\" cy=\"{height}\"/>"
            + "<wp:effectExtent l=\"0\" t=\"0\" r=\"0\" b=\"0\"/>"
            + "<wp:wrapNone/>"
            + $"<wp:docPr id=\"{id}\" name=\"{Attr(shapeName)}\"/>"
            + "<wp:cNvGraphicFramePr><a:graphicFrameLocks noChangeAspect=\"0\"/></wp:cNvGraphicFramePr>"
            + "<a:graphic><a:graphicData uri=\"http://schemas.microsoft.com/office/word/2010/wordprocessingShape\">"
            + "<wps:wsp>"
            + "<wps:cNvSpPr txBox=\"1\"/>"
            + "<wps:spPr>"
            + $"<a:xfrm{rot}><a:off x=\"0\" y=\"0\"/><a:ext cx=\"{width}\" cy=\"{height}\"/></a:xfrm>"
            + "<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom>"
            + "<a:noFill/>"
            + "<a:ln><a:noFill/></a:ln>"
            + "</wps:spPr>"
            + "<wps:txbx><w:txbxContent><w:p>"
            + "<w:pPr><w:snapToGrid w:val=\"0\"/>"
            + "<w:spacing w:before=\"0\" w:after=\"0\" w:line=\"240\" w:lineRule=\"auto\"/>"
            + "<w:jc w:val=\"center\"/></w:pPr>"
            + "<w:r><w:rPr>"
            + $"<w:rFonts w:ascii=\"{font}\" w:hAnsi=\"{font}\" w:eastAsia=\"{font}\" w:cs=\"{font}\"/>"
            + $"<w:sz w:val=\"{halfPoints}\"/><w:szCs w:val=\"{halfPoints}\"/>"
            + "<w:lang w:val=\"zh-CN\" w:eastAsia=\"zh-CN\"/>"
            + "</w:rPr>"
            + $"<w:t xml:space=\"preserve\">{XmlText(name)}</w:t>"
            + "</w:r></w:p></w:txbxContent></wps:txbx>"
            + "<wps:bodyPr rot=\"0\" spcFirstLastPara=\"0\" vertOverflow=\"overflow\" horzOverflow=\"overflow\" vert=\"horz\" wrap=\"square\" lIns=\"0\" tIns=\"0\" rIns=\"0\" bIns=\"0\" anchor=\"ctr\" anchorCtr=\"0\" upright=\"0\">"
            + "<a:noAutofit/>"
            + "</wps:bodyPr>"
            + "</wps:wsp></a:graphicData></a:graphic></wp:anchor></w:drawing></w:r>";
    }

    private static string Guide(uint id, string shapeName, double xPt, double yPt, double widthPt)
    {
        var x = Emu(xPt);
        var y = Emu(yPt);
        var width = Emu(widthPt);

        return "<w:r><w:drawing>"
            + $"<wp:anchor distT=\"0\" distB=\"0\" distL=\"0\" distR=\"0\" simplePos=\"0\" relativeHeight=\"{260000000 + id}\" behindDoc=\"0\" locked=\"0\" layoutInCell=\"1\" allowOverlap=\"1\">"
            + "<wp:simplePos x=\"0\" y=\"0\"/>"
            + $"<wp:positionH relativeFrom=\"page\"><wp:posOffset>{x}</wp:posOffset></wp:positionH>"
            + $"<wp:positionV relativeFrom=\"page\"><wp:posOffset>{y}</wp:posOffset></wp:positionV>"
            + $"<wp:extent cx=\"{width}\" cy=\"0\"/>"
            + "<wp:effectExtent l=\"0\" t=\"0\" r=\"0\" b=\"0\"/>"
            + "<wp:wrapNone/>"
            + $"<wp:docPr id=\"{id}\" name=\"{Attr(shapeName)}\"/>"
            + "<wp:cNvGraphicFramePr><a:graphicFrameLocks/></wp:cNvGraphicFramePr>"
            + "<a:graphic><a:graphicData uri=\"http://schemas.microsoft.com/office/word/2010/wordprocessingShape\">"
            + "<wps:wsp>"
            + "<wps:cNvCnPr/>"
            + "<wps:spPr>"
            + $"<a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"{width}\" cy=\"0\"/></a:xfrm>"
            + "<a:prstGeom prst=\"straightConnector1\"><a:avLst/></a:prstGeom>"
            + "<a:ln w=\"12700\" cap=\"flat\" cmpd=\"sng\">"
            + "<a:solidFill><a:srgbClr val=\"808080\"/></a:solidFill>"
            + "<a:prstDash val=\"dash\"/>"
            + "</a:ln>"
            + "</wps:spPr>"
            + "<wps:bodyPr/>"
            + "</wps:wsp></a:graphicData></a:graphic></wp:anchor></w:drawing></w:r>";
    }

    private static string Emu(double points) =>
        Math.Round(points * 12700.0, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture);

    private static string Attr(string value) =>
        value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);

    private static string XmlText(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (ch < 0x20 && ch is not '\t' and not '\n' and not '\r')
                continue;
            if (ch is '\uFFFE' or '\uFFFF')
                continue;

            switch (ch)
            {
                case '&':
                    builder.Append("&amp;");
                    break;
                case '<':
                    builder.Append("&lt;");
                    break;
                case '>':
                    builder.Append("&gt;");
                    break;
                default:
                    builder.Append(ch);
                    break;
            }
        }

        return builder.ToString();
    }
}
