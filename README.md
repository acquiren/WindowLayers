<div align="center">
  <img src="WindowLayers.png" width="104" alt="WindowLayers 图标" />
  <h1>WindowLayers</h1>
  <p><strong>需要的窗口留在最前，不需要的窗口退到最后。</strong></p>
  <p>一个专注于窗口置顶与持续置底的 Windows 托盘工具。</p>
  <p>
    <a href="#快速开始">快速开始</a> ·
    <a href="#使用指南">使用指南</a> ·
    <a href="#设置与行为">设置与行为</a> ·
    <a href="#构建与开发">构建与开发</a>
  </p>
</div>

---

打开多个程序时，总要翻找刚被盖住的窗口？WindowLayers 用两组快捷键调整**当前活动窗口**的层级。置顶窗口始终在前；置底窗口即使再次被激活，工具也会尝试把它保持在其他普通应用窗口下方。两个状态分别只保留一个目标，彩色动态边框让当前状态一眼可见。

## 特性

| 功能 | 具体行为 |
| --- | --- |
| 置顶与置底 | 对当前活动窗口切换状态；各只保留一个目标窗口 |
| 持续置底 | 监听前台窗口变化，并定时调整置底窗口的位置 |
| 动态边框 | 分别设置置顶、置底颜色；可调整粗细、速度和动画开关 |
| 托盘运行 | 启动后不弹出设置界面；从托盘打开设置、取消状态或退出 |
| 自定义快捷键 | 在设置界面录入组合键，保存后立即生效 |
| 开机自启动 | 直接从托盘右键菜单切换，无需打开设置界面 |

工具使用 Windows 窗口 API 调整层级，不向目标程序注入代码；设置保存在本机，无需联网使用。

## 快速开始

从 [Releases](https://github.com/acquiren/WindowLayers/releases/latest) 下载 `WindowLayers-v0.1.0-win-x64.zip`，解压后运行 `WindowLayers.exe`。发行包无需安装 .NET SDK，但电脑需要安装 [.NET 6 Desktop Runtime（x64）](https://dotnet.microsoft.com/download/dotnet/6.0)。启动后请在系统托盘查找图标。

也可以在 Windows 上安装 .NET 6 SDK，从源码构建：

在 PowerShell 中运行：

```powershell
git clone https://github.com/acquiren/WindowLayers.git
cd WindowLayers
dotnet build -c Release
.\bin\Release\net6.0-windows\WindowLayers.exe
```

普通构建同样依赖 **.NET 6 Desktop Runtime**；复制给其他电脑时，请保留整个 `bin/Release/net6.0-windows/` 输出目录，不能只复制 `.exe`。

## 使用指南

1. 切换到想要调整的程序窗口。
2. 按下对应快捷键；再次对同一窗口按下可取消该状态。
3. 双击托盘图标，或右键选择“打开设置”，修改快捷键和边框效果。

| 操作 | 默认快捷键 / 入口 |
| --- | --- |
| 切换置顶 | `Ctrl + Alt + T` |
| 切换置底 | `Ctrl + Alt + B` |
| 打开设置 | 双击托盘图标，或托盘右键菜单 |
| 取消置顶 / 取消置底 | 托盘右键菜单 |
| 开机自启动 / 退出 | 托盘右键菜单 |

快捷键输入框接受包含修饰键的组合键；置顶和置底不能设置成相同的组合。点击“保存设置”后，新组合键立即注册。

## 设置与行为

- **唯一置顶名额**：打开设置界面时，会取消原来置顶的窗口，并将设置界面置顶。隐藏设置界面后释放名额，原窗口不会自动重新置顶。
- **持续置底**：工具会在置底窗口成为前台或窗口顺序变化后尝试调整它的位置；这不是 Windows 提供的永久置底属性。
- **配置文件**：`%APPDATA%\WindowLayers\settings.json`。
- **开机自启动**：在当前用户的 Windows 启动项中记录程序路径。移动程序后，可在托盘菜单中关闭并重新开启此选项。
- **单实例**：已有一个进程运行时，再次启动会提示并退出。

### 已知限制

- 更改管理员权限窗口的层级时，可能需要以管理员身份运行本工具。
- 独占全屏程序和特殊窗口管理方式可能影响置顶、置底或边框效果。
- 当前仅支持 Windows。

## 构建与开发

项目使用 **C#、.NET 6 和 Windows Forms**，核心逻辑位于 [`Program.cs`](Program.cs)，项目配置位于 [`WindowLayers.csproj`](WindowLayers.csproj)。

```powershell
dotnet build WindowLayers.csproj -c Release
```

`WindowLayers.ico` 是嵌入程序的图标，`WindowLayers.png` 用于展示。`bin/` 存放构建输出，`obj/` 存放构建中间文件，均不提交到 Git。

有问题或改进建议，可以在 [Issues](https://github.com/acquiren/WindowLayers/issues) 中提出。

## 许可证

本仓库目前没有 `LICENSE` 文件。
