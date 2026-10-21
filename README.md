# 桌签生成器

开会、接待、培训、宴席，桌上要给每人立一块姓名牌。牌的两面都要能看见同一个名字，A4 又比透明夹子大，多出来的边得折进去。用 Word 一页一页排，上半页要转 180 度，折线要自己量，换一份名单就重做一遍。

桌签生成器读一份名单，一行一个名字，每个人出一页 A4：上半页倒着印，下半页正着印。姓名外侧的灰色虚线会印出来，沿这条线把多余的纸折进夹子。页中间的对折痕迹只在预览里，不写进文档。

默认是宋体、150 磅，两个名字中心相距 14 厘米，虚线在字外 0.5 厘米。字体、字号、相距、虚线距都可以改。生成前可以看平面，也可以看折进夹子的过程。

开发者 c0ldjc，版本 1.0。介绍页是仓库根目录的 `桌签生成器.html`。

## 使用

1. 准备一份 txt，一行一个名字。支持 UTF-8 和 GBK。
2. 打开程序，调整字体、字号、相距、虚线距。
3. 确认预览后生成文档，打印，沿灰色虚线折好，放进透明桌签夹。

## 编译

需要 .NET 10 SDK（含 Windows 桌面）。

```bash
dotnet test DeskSign.slnx
dotnet build src/DeskSign/DeskSign.csproj -c Release
```

单文件程序：

```bash
dotnet publish src/DeskSign/DeskSign.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:DebugSymbols=false -o 单文件
```

## 许可

[MIT](LICENSE)
