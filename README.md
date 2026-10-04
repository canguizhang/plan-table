# 我的计划表

一个不用联网、不用安装的 Windows 待办计划小工具。双击 exe 就能用，数据保存在本地纯文本文件里。

用 C# + WinForms 编写，整个程序就是**一个源码文件**（498 行），适合当作练手参考。

## 简介

这是**编程新手第一次用 DSH（AI 编程助手）辅助完成**的一个 Windows 小程序。

从"我想要一个自己的计划表"这个念头开始，到能双击运行的 exe，再到这份 498 行的源码——作者是刚开始学编程的小白，程序是在 DSH 的帮助下一点点做出来的。

功能都很朴素，但每一条都是自己用得上才加的：加计划、打勾完成、写备注、看统计。程序不联网、不用安装、不写注册表、不需要管理员权限。

如果你也刚开始学编程，希望这个小项目能给你一点信心 —— **有个想法，就能把它做出来。**

## 安全说明

这个程序很小，但边界很清楚。下面每一条都是逐行核查 `开发文件/计划表源码.cs` 之后得出的结论：

| 它做什么 | 证据 |
| --- | --- |
| **不联网** —— 完全没有联网能力 | 源码中网络 API 命中数为 **0**：没有 `System.Net`、没有 HttpClient / WebRequest / Socket / Dns |
| **不写注册表、不设开机自启** | 注册表相关调用命中数为 **0**（无 `Microsoft.Win32.Registry`） |
| **不启动其他程序** | 无 `Process.Start` 之类的调用 |
| **只碰自己的文件夹** | 全部文件读写只有一处：程序同目录下的 `计划数据.txt`（`Application.StartupPath`） |
| **不要管理员权限** | 清单文件声明 `requestedExecutionLevel level="asInvoker"`，只以普通用户身份运行 |
| **不含任何身份信息** | 源码、exe、文档里都没有账号、邮箱、令牌、本机路径 |

换句话说：把它复制给任何人，对方都无法通过它碰到你的电脑、你的账号或你的其他文件。关掉窗口就完全退出，不会驻留后台。

## 功能

| 功能 | 怎么用 |
| --- | --- |
| 添加计划 | 在窗口顶部的输入框打字，按回车（或点右侧「添加」） |
| 标记完成 | 点条目最前面的小方框，标题变灰并划横线；再点一次取消 |
| 写备注 | 点条目标题文字，下方展开备注框，可写详细步骤，**边写边自动保存** |
| 删除 | 点条目最右侧的 `×`，会先弹窗确认 |
| 重命名 | 在条目上点鼠标右键 → 重命名 |
| 查看统计 | 窗口底部显示「共 N 条 · 已完成 N 条 · 剩余 N 条」 |
| 清除已完成 | 点底部「清除已完成」，一次删掉所有打过勾的条目（会先确认） |

## 直接使用

下载 `我的计划表.exe`，双击运行即可。

- 系统要求：Windows 7 及以上（依赖系统自带的 .NET Framework 4.x）
- 这是**未签名**的小程序，杀毒软件偶尔会提示一次，选择「仍要运行」即可
- 程序不联网、不写注册表、不动系统文件、不需要管理员权限；关掉窗口就完全退出，不会在后台运行

## 数据存在哪

程序所在目录下的 `计划数据.txt`，是**纯文本**，用记事本就能打开看。

- 备份：把这个文件复制一份放到别的地方（U 盘、网盘）即可
- 换电脑：把整个文件夹拷过去，双击 exe 照样能用
- 这个文件是程序自动创建的（首次运行、或保存计划时生成），所以仓库里没有它

## 从源码编译

用 Windows 自带的 C# 编译器 `csc.exe`（.NET Framework 4.x 自带，无需装 Visual Studio）：

```bat
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe ^
  /out:我的计划表.exe ^
  /r:Microsoft.VisualBasic.dll ^
  /win32icon:开发文件\计划表图标.ico ^
  /win32manifest:开发文件\程序清单.manifest ^
  开发文件\计划表源码.cs
```

编译完成会在当前目录生成 `我的计划表.exe`。

> 注意 `/r:Microsoft.VisualBasic.dll` 不能省：重命名功能用到了 `Microsoft.VisualBasic.Interaction.InputBox`，而它不在 csc 的默认引用里，漏掉会报 `CS0234`。

### 可选：编译出一个不带「编译时间戳」的 exe

用上面系统自带的 csc 编译，exe 里会写入**编译时刻**（PE 时间戳）。它不影响任何功能，只是会让人知道你大概什么时候编译的。

本仓库发布的 exe 是用 **.NET SDK 自带的 Roslyn 编译器 + `/deterministic`** 编译的，时间戳变成内容哈希、与时间无关：

```bat
dotnet "%ProgramFiles%\dotnet\sdk\<版本号>\Roslyn\bincore\csc.dll" /nologo /deterministic /nostdlib+ ^
  /target:winexe /out:我的计划表.exe ^
  /r:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\mscorlib.dll ^
  /r:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.dll ^
  /r:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Core.dll ^
  /r:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Drawing.dll ^
  /r:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Windows.Forms.dll ^
  /r:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\Microsoft.VisualBasic.dll ^
  /win32icon:开发文件\计划表图标.ico ^
  /win32manifest:开发文件\程序清单.manifest ^
  开发文件\计划表源码.cs
```

## 目录结构

```
计划表/
├── 我的计划表.exe            # 编译好的程序，双击使用
├── 使用说明.txt              # 给普通用户看的图文说明
├── 计划数据.txt              # 计划数据（程序自动生成，不提交到仓库）
└── 开发文件/
    ├── 计划表源码.cs         # 全部源码，单文件
    ├── 计划表图标.ico        # 程序图标
    └── 程序清单.manifest     # 清单文件（不用管理员权限 + 高 DPI 适配）
```

## 实现要点

- **高 DPI 适配**：启动时读取显示器 DPI，按比例缩放所有控件尺寸（`Ui.Detect` / `Ui.Px`）
- **界面用代码手写**：没有用可视化设计器，所有控件在 `InitializeComponent` 里手工创建
- **数据格式**：`计划数据.txt` 用 UTF-8 存储，每行一条计划，字段之间用制表符分隔
- **容错**：读取数据失败时不崩溃，会备份原文件并提示，程序按空表打开
- **零依赖**：只用 .NET Framework 自带的类库，没有第三方包
