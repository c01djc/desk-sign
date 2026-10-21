namespace DeskSign.Core;

public static class FontNames
{
    public static string ForDocument(string fontName) => fontName switch
    {
        "SimSun" or "NSimSun" => "宋体",
        "SimHei" => "黑体",
        "Microsoft YaHei" or "Microsoft YaHei UI" => "微软雅黑",
        "KaiTi" => "楷体",
        "FangSong" => "仿宋",
        "DengXian" => "等线",
        "LiSu" => "隶书",
        "YouYuan" => "幼圆",
        _ => fontName
    };
}
