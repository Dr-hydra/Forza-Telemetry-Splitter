# Forza 遥测分流器

[English](README.md) · [日本語](README.ja.md) · [Français](README.fr.md) · [Deutsch](README.de.md) · [Español](README.es.md) · **简体中文**

把 Forza 的遥测数据同时发送给多个工具。

Forza Horizon 6 的 “Data Out” 遥测只能发送到一个 IP 和端口。这意味着你常常必须在
[VirtualTCU](https://github.com/Forza-Love/fh6-virtual_tcu)、自动调校工具、仪表盘等工具之间做选择，
无法让它们同时接收数据。

Forza 遥测分流器位于中间：它在自己的端口接收 Forza 遥测数据，然后把每个数据包原样转发到任意数量的本地工具。
转发开销低于 1 毫秒，数据不会被修改，因此每个工具都会像直接连接 Forza 一样工作。

本项目与 Turn 10、Playground Games 或 Microsoft 无隶属关系，也未获得其认可。“Forza” 是 Microsoft 的商标。

## 功能

| 功能 | 说明 |
|------|------|
| 分流转发 | 把 Forza 遥测数据转发到任意数量的目标，数据包保持原样。 |
| 多游戏支持 | 支持 Forza Horizon 4/5/6 与 Forza Motorsport（7、2023），并在应用内自动识别游戏名称。 |
| 状态指示 | 每个目标都有状态点（转发中 / 空闲 / 已禁用 / 错误），方便快速确认哪些工具正在接收数据。 |
| 活动图表 | 第二个标签页显示最近一段时间的数据包/秒，仅保存在内存中，不会写入磁盘。 |
| 状态叠加层 | 屏幕右上角的小胶囊显示“已连接”或“无数据”，并显示实时挡位和速度。可从托盘开关。 |
| 速度单位 | 支持 mph 或 kph，默认根据 Windows 区域选择，也可在应用中切换。 |
| 工具预设 | 可以从常见遥测工具预设中添加目标，也可以自定义 IP 和端口。 |
| 托盘常驻 | 像 VirtualTCU 一样在系统托盘安静运行。 |
| 无需管理员权限 | 只使用本地 UDP，不需要 UAC 提权。 |
| 随 Windows 启动 | 应用内可开启，便携版同样支持。 |
| 会话录制 | 可把实时遥测保存为 `.fts` 文件，便于提交 bug 报告和复现问题。 |
| 单文件发布 | 提供每用户安装器或便携单文件 `.exe`，无需额外安装 .NET 运行时。 |

## 支持的游戏

分流器支持任何带有 “Data Out” 遥测功能的 Forza 游戏。它会根据遥测格式自动识别正在运行的游戏。

| 游戏 | 遥测格式 | 实时挡位/速度读数 |
|------|----------|-------------------|
| Forza Horizon 6 | Car Dash | 支持 |
| Forza Horizon 5 | Car Dash | 支持 |
| Forza Horizon 4 | Car Dash | 支持 |
| Forza Motorsport (2023) | Dash | 支持 |
| Forza Motorsport 7 | Dash | 支持 |

“Sled” 遥测格式也会被转发，但它不包含仪表盘字段，因此不会显示挡位和速度读数。

## 安装

推荐使用安装器：

1. 从 [Releases](../../releases) 页面下载 `ForzaTelemetrySplitterInstaller.exe`。
2. 右键文件，选择“属性”，在“常规”选项卡底部勾选“解除锁定”，然后点击“确定”。这可以避免 SmartScreen 的“Windows 已保护你的电脑”提示。仍看到提示时可阅读 [docs/SMARTSCREEN.md](docs/SMARTSCREEN.md)。
3. 运行安装器。它是每用户安装，不会弹出管理员权限提示，并可选择桌面快捷方式和随 Windows 启动。
4. 安装完成后会在系统托盘启动。

高级用户 / 免安装：下载 `ftsPortable.exe` 直接运行即可。多数用户推荐使用安装器。

如果找不到托盘图标，Windows 可能默认隐藏了新图标。点击任务栏右下角的小箭头，再把图标拖到任务栏上即可固定显示。

## 游戏内设置

应用会自动开始分流，你只需要把 Forza 指向它：

1. 从托盘打开应用。默认监听端口是 `44405`，并已默认转发到 VirtualTCU 的常用端口 `5555`。
2. 在 Forza 中打开 Data Out（Horizon 系列：设置、HUD 和游戏玩法、Data Out；Motorsport 位于玩法/HUD 选项中）：
   - Data Out: ON
   - IP Address: 127.0.0.1
   - Port: 44405
   - Packet Format: Car Dash（Horizon）或 Dash（Motorsport）
3. 其他工具保持原来的设置。分流器会转发到每个工具已在使用的端口。需要添加更多工具时，在应用中点击“添加”，选择预设或输入自定义 IP/端口。
4. 开始驾驶。遥测数据流动时，右上角叠加层会变绿，每个启用的工具都会同时收到数据。

分流器使用自己的端口 `44405`，不会占用其他工具正在监听的端口。若看到“端口已被占用”提示，可在应用中修改监听端口，并让 Forza 的 Data Out 端口与它保持一致。

## 默认端口

| 项目 | IP | 端口 |
|------|----|------|
| Forza Data Out，进入分流器 | 127.0.0.1 | 44405 |
| 转发到 VirtualTCU（保持不变） | 127.0.0.1 | 5555 |
| 转发到调校工具示例（默认关闭） | 127.0.0.1 | 9999 |

## 支持的工具

任何读取 Forza 实时 UDP 遥测的工具都可以使用。分流器会转发到各工具自己的常用端口，因此通常不需要修改工具设置。

| 工具 | 默认端口 | 说明 |
|------|----------|------|
| [VirtualTCU](https://github.com/Forza-Love/fh6-virtual_tcu) | 5555 | 自动换挡。保持 5555 不变。 |
| [ForzaDash](https://github.com/himanshupapola/ForzaDash) | 1234 | 开源 FH6 遥测仪表盘。 |
| [Forza-data-tools](https://github.com/richstokes/Forza-data-tools) | 9999 | 开源 CLI 和浏览器仪表盘。 |
| [SIM Dashboard](https://www.stryder-it.de/simdashboard/) | 5685 | 手机或平板仪表盘。使用设备 IP。 |
| [SimHub](https://www.simhubdash.com/) | 20777 | 仪表盘和效果套件。 |
| [co-driver](https://github.com/Ojansen/co-driver) | 5300 | MIT 许可的马力机和调校工作台。 |
| [Tune It Yourself](https://www.tuneityourself.co.uk/) | Wi-Fi | 实时遥测自动调校工具（付费）。使用设备 IP，不是 127.0.0.1。 |

ForzaTune 这类计算器调校工具不读取遥测，因此分流器对它们没有作用。

## 叠加层

右上角的小胶囊会自动定位到主屏幕。绿色表示正在收到有效 Forza 数据包，并显示当前挡位和速度；红色表示没有数据（例如你在菜单中，或 Forza 的 Data Out 没有指向分流器）。

请使用无边框或窗口化模式运行 Forza。真正的全屏模式可能隐藏任何叠加层，这是 Windows 的限制。

## 更新

应用不会后台自动更新。托盘菜单里的“检查更新”会打开 Releases 页面。若有新版，下载新的安装器并运行即可原地升级，设置会被保留。

## 录制会话

“录制”按钮会把实时遥测保存为 `.fts` 文件。这是数据包的逐字节副本，可用于 bug 报告和复现。录制只会写入你选择的位置，不会上传。文件每分钟会增长数 MB，完成后请停止录制。

## 设置文件

设置位于 `%APPDATA%\ForzaTelemetrySplitter\config.json`，包括监听端口、目标列表和叠加层开关。删除它会在下次启动时恢复默认设置。

## 更多

- [从源码构建](docs/BUILDING.md)
- [Windows SmartScreen 警告](docs/SMARTSCREEN.md)
- [报告问题](docs/REPORTING-BUGS.md)
- [贡献指南](CONTRIBUTING.md)
- [许可证（MIT）](LICENSE)

## 测试环境

Windows 10 和 Windows 11。Forza Horizon 4/5/6 以及 Forza Motorsport（7、2023）会根据数据包格式自动识别。

应用界面支持 English、日本語、Français、Deutsch、Español 和简体中文，会根据 Windows 语言自动选择，也可在应用中手动切换。
