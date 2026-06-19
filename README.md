# v2rayN FlowLens

中文 | [English](README.en.md)

<img src="V2rayN.FlowLens.App/Assets/AppIcon.png" width="96" alt="v2rayN FlowLens icon" />

v2rayN FlowLens 是一个 Windows 桌面工具，用来把 v2rayN / Xray / sing-box 的访问日志和 Windows TCP / ETW 数据结合起来，尽量还原：

```text
本机应用 -> v2rayN 本地代理入口 -> proxy / direct 路由
```

它是**只读监控工具**。它不会替代 v2rayN，不会修改 v2rayN 配置，不会管理订阅或节点，不会抓包，也不会上传日志、域名、账号或订阅信息。

## 当前状态

- 推荐日常模式：`NormalProxy`
- 可选观察模式：`Tun`
- 当前发布包：`V2.3.1`
- 支持语言：English / 简体中文，切换后重启生效
- 发布形态：self-contained `win-x64` 便携 ZIP，不需要 .NET SDK

## 快速使用

1. 从 Release 下载并解压 `V2rayN.FlowLens-2.3.1-win-x64.zip`。
2. 双击 `V2rayN.FlowLens.exe`。
3. 如果需要 ETW 字节统计，接受 Windows 管理员 UAC 提示。
4. 在 v2rayN 中开启 Core 日志，并把日志级别设为 `info`，然后重启 v2rayN Core。
5. 在 FlowLens 中选择 v2rayN 根目录或 `guiLogs` 目录。
6. 确认代理端口和 v2rayN 本地端口一致，例如 `10808` 或 `10808,10809`。
7. TUN 关闭时使用 `NormalProxy`；TUN 开启时可切到 `Tun` 查看保守归因和诊断证据。

FlowLens 会保存非敏感设置到：

```text
%LocalAppData%\V2rayN.FlowLens\settings.json
```

每日聚合历史保存到：

```text
%LocalAppData%\V2rayN.FlowLens\history\yyyy-MM-dd.json
```

这些文件只保存设置和聚合统计，不保存完整访问日志、原始连接明细、节点、订阅、账号或凭据。

## 两种归因模式

### NormalProxy

这是默认模式，也是目前最可靠的模式。

FlowLens 会读取当前 Windows TCP 连接，找出哪些应用连接到了 v2rayN 本地代理端口，例如 `127.0.0.1:10808`。然后它会用应用连接时的本地源端口，匹配 v2rayN Core 日志中的记录：

```text
2026/06/13 16:39:10 from 127.0.0.1:9852 accepted //www.google-analytics.com:443 [socks -> proxy]
```

匹配成功后，FlowLens 可以显示：

- 应用名和 PID
- 目标域名或 IP
- `proxy` / `direct` / `block` / `unknown`
- 实时连接
- 本次统计
- 今日统计
- 历史聚合
- CSV 导出
- 诊断信息

### Tun

TUN 模式是保守归因模式，不承诺精确。

它会使用 Windows TCP 连接、ETW TCP 字节、v2rayN / Xray / sing-box 路由日志、目标 IP/端口、域名证据和时间窗口做近似匹配。

置信度含义：

- `Matched`：目标 IP + 端口 + 时间窗口唯一命中
- `Probable`：域名日志无法映射 IP，但时间窗口和端口只有一个候选
- `Ambiguous`：多个候选都可能匹配，FlowLens 拒绝硬猜
- `Unknown`：证据不足

`Ambiguous` 和 `Unknown` 可以出现在实时连接或诊断里，但不会进入 Applications / Session / Today / History 的确定应用统计。

## 统计口径

FlowLens 的流量统计不是运营商账单，也不是 v2rayN 节点总流量。

NormalProxy 下主要统计的是：

```text
应用进程 -> 本地 v2rayN 代理端口
```

不统计：

- v2rayN Core 到远端节点的二次出站流量
- UDP
- 包内容
- 启动 FlowLens 之前已经发生的流量
- 不可靠的 `Idle.exe` / PID 0 行

所以 FlowLens 的 Today 不应该和 v2rayN 主界面的“今日上传/今日下载”直接对齐。FlowLens 的价值是帮助判断“哪个应用通过代理访问了什么目标，以及大致产生了多少本地入口流量”。

## 诊断

当归因看起来不对，优先打开 Diagnostics 页。

重点看：

- `Admin`：是否管理员运行
- `ETW`：字节计数是否启动
- `Access log`：是否找到日志文件
- `Log health`：是否解析到了 Core 路由日志
- `Proxy ports`：当前代理端口是否正确
- `Match stats`：`Matched` / `PortOnly` / `LogOnly` / `Unknown` 数量
- `Confidence`：TUN 模式下的置信度分布
- `TUN evidence JSON`：复制最近一次 TUN 诊断证据

如果只看到 GUI 启动日志，没有类似 `[socks -> proxy]` 的 Core 路由记录，FlowLens 仍可识别部分应用连接，但无法判断 `proxy` / `direct`。

## 构建和测试

要求：

- Windows
- .NET 8 SDK

常用命令：

```powershell
dotnet build .\V2rayN.FlowLens.sln --no-restore
dotnet test .\V2rayN.FlowLens.sln --no-restore
```

生成 V2.3.1 便携发布包：

```powershell
.\scripts\package-release.ps1 -Version 2.3.1
```

输出：

```text
artifacts\V2rayN.FlowLens-2.3.1-win-x64\
artifacts\V2rayN.FlowLens-2.3.1-win-x64.zip
```

开发运行：

```powershell
dotnet run --project .\V2rayN.FlowLens.App\V2rayN.FlowLens.App.csproj
```

## 手工验收

NormalProxy：

1. 关闭 v2rayN TUN。
2. 确认系统代理由 v2rayN 自动配置。
3. 启用 Core 日志，日志级别为 `info`。
4. 启动 FlowLens，选择 v2rayN 根目录或 `guiLogs`。
5. 用浏览器访问 `google.com`、`github.com`、`baidu.com` 等网站。
6. 确认 Applications / Live Connections 中显示浏览器、Codex、git 等原始应用，而不是全部归到 `xray.exe` 或 `sing-box.exe`。
7. 确认 `proxy` / `direct` 路由能正常出现。
8. 确认 Session / Today 能累计，CSV 能导出。

TUN：

1. 在 v2rayN 中开启 TUN。
2. FlowLens 切到 `Tun` 模式。
3. 保持 Core 日志为 `info`。
4. 访问多个网站，观察 `Matched` / `Probable` / `Ambiguous` / `Unknown`。
5. 多应用同时访问同一站点时，确认 FlowLens 不会把不确定证据硬塞给某个应用。
6. 需要排查时复制 `TUN Evidence JSON`。

## 相关参考

FlowLens 不是通用防火墙，也不是抓包工具。它的定位是 v2rayN 代理流量归因面板。

参考过的项目：

- [OpenNetMeter](https://github.com/Ashfaaq18/OpenNetMeter)：Windows 流量统计和历史视图参考
- [WhoYouCalling](https://github.com/H4NM/WhoYouCalling)：ETW / DNS 关联思路参考
- [Sniffnet](https://github.com/GyulyVGC/sniffnet)：网络监控 UX 参考
- [Portmaster](https://github.com/safing/portmaster)、[simplewall](https://github.com/henrypp/simplewall)：产品说明和权限提示参考，不复制 GPL 源码

更多记录见 [docs/reference-analysis.md](docs/reference-analysis.md)。

## 后续方向

- 更强的 TUN 证据采样和解释
- DNS/ETW 补充证据研究
- 更清晰的桌面验收截图和 Release 页面
- 更稳定的窗口视觉验收自动化
