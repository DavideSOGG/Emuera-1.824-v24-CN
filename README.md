# Emuera 1.824+v24 汉化修正版

本项目是基于 MinorShift 的 Emuera 1.824+v24 源码制作的汉化修正版，并非 Emuera 官方发布版本。原版作者为 MinorShift、妊）|дﾟ)の中の人。

原项目地址：http://osdn.jp/projects/emuera/

## 本次修改

- 将程序菜单、设置窗口、常见提示、脚本解析警告和运行错误信息翻译为中文，并修正旧汉化版本中的漏翻、错翻和中日文本混杂问题。
- 保留 ERB/CSV 协议关键字、脚本标识符、默认字体、默认标题菜单文本及全角字符宽度测量逻辑，避免破坏游戏兼容性。
- 游戏脚本仍按 Shift-JIS 等原有规则读取；配置文件可使用带 BOM 的 UTF-8 读写，减少在非日文系统区域设置下使用时的转区需求。
- 目标框架由 .NET Framework 4.5 调整为 .NET Framework 4.6.2，并保留 `Emuera.exe.config` 的运行时声明。
- 修复鼠标左键点击“文件(F)”或“帮助(H)”后菜单文字变白、消失的问题。
- 清理部分冗余表达式和项目配置，并修正数组维度检查中的重复判断。

## 运行环境

需要安装 **.NET Framework 4.6.2 或兼容的更高版本运行时**。

## 使用方法

1. 将 `Emuera.exe` 和 `Emuera.exe.config` 放在游戏根目录，即原本 `eramaker.exe` 所在、同时包含 `CSV` 和 `ERB` 文件夹的位置。
2. 双击 `Emuera.exe` 启动。无需将程序放入单独的子文件夹。
3. 首次启动后会按需要生成 `emuera.config`。日志、宏和存档目录由游戏及配置决定，请勿误删存档。
4. 游戏正文、角色名和脚本内容仍由游戏自身决定。本修正版翻译的是 Emuera 引擎界面和诊断信息，不会自动翻译游戏剧本。

## 发布包内容

- `Emuera.exe`：汉化修正版主程序。
- `Emuera.exe.config`：.NET Framework 4.6.2 运行时配置。
- `README.md`：本说明。
- `license.txt`：原版许可证，未作修改。

## 源码与构建

源码包包含 `Emuera.sln`、完整 C# 源码、项目文件、原始说明和许可证。使用安装有 **.NET Framework 4.6.2 Developer Pack / 目标包** 的 Visual Studio 打开 `Emuera.sln`，选择 `Release|Any CPU` 构建即可。

源码包不包含测试游戏、本机参考程序集包、编译器包、`bin`、`obj` 或其他构建产物。

## 验证情况

已通过 Release 构建、Shift-JIS 文本读取、UTF-8 配置读写，以及中文和日文示例游戏的脚本解析测试。已实际启动中文和 Shift-JIS 日文游戏，验证标题及游戏脚本运行，并检查“文件(F)”和“帮助(H)”的点击显示回归。示例脚本自身的警告仍会正常显示，没有为通过测试而屏蔽。

## 许可证与致谢

本软件基于原版 Emuera 修改，依据原版 zlib/libpng 许可证发布。`license.txt` 中的版权声明和许可条件必须保留。

感谢 MinorShift 及 Emuera 原作者。请勿将本修正版宣称为官方原版，也不要向 eramaker 作者反馈 Emuera 的相关问题。
