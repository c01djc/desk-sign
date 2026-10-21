# 桌签生成器

把一份名单做成可对折的 A4 桌签。

开会、接待时要给每人立一块姓名牌：两面都得是正的同一个名字，纸又比透明夹子大。不用在 Word 里一页一页排。

下载：[桌签生成器.exe](https://github.com/c01djc/desk-sign/releases/download/v1.0/桌签生成器.exe)（64 位 Windows，一个文件）

版本 1.0 · [MIT](LICENSE)

## 编译

需要 .NET 10 SDK（含 Windows 桌面）。

```bash
dotnet test DeskSign.slnx
dotnet publish src/DeskSign/DeskSign.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:DebugSymbols=false -o 单文件
```
