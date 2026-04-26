# YHSleepRunner

一个本地 OCR 自动化启动器。界面只保留两个操作按钮：

- `店长特供2-8`：启动当前仓库的自动化流程。
- `停止`：终止本次启动的 PowerShell / dotnet 进程树。

项目逻辑基于屏幕截图、OCR 识别和 Win32 输入，不依赖模板图片或图片资产。

## 快速使用

下载打包版：

1. 打开 GitHub 仓库的 Releases 页面。
2. 下载 `YHSleepRunner-win-x64.zip`。
3. 解压后运行 `YHSleepRunner.exe`。

打开界面：

```powershell
dotnet run --project .\src\YihuanRunner
```

或显式指定 UI 模式：

```powershell
dotnet run --project .\src\YihuanRunner -- --ui
```

界面中的 `店长特供2-8` 会执行：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-yihuan.ps1
```

## 命令行调试

只检测当前画面，不执行点击：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-yihuan.ps1 -Probe -Snapshot .\artifacts\probe.png
```

只运行一轮：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-yihuan.ps1 -Once
```

持续循环：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-yihuan.ps1
```

如果需要校准点击坐标：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-yihuan.ps1 -HammerX 0.0667 -HammerY 0.4620
```

## 常见问题

### 流程退出，错误码 3

这通常表示 Windows 权限隔离拦截输入：启动器权限低于目标窗口权限。请尝试以下任一方式：

- 右键 `YHSleepRunner.exe`，选择“以管理员身份运行”。
- 或者用普通权限启动目标窗口，保持两边权限一致。

## 设计说明

- `Program` 负责入口分流：无参数或 `--ui` 打开界面；带自动化参数时保留原命令行 OCR 流程。
- `Workflows` 目录保存自动化流程定义、流程目录和进程控制器，后续新增按钮时只需要注册新的流程定义。
- `Forms` 目录保存界面主题和控件。配色沿用暖白背景、橙色主按钮、浅边框和灰棕文字。
- 自动化识别全部走 OCR，不提交截图、模板图或其他图片资产。

## 免责声明

本项目仅供个人学习、研究和本地自动化测试使用。使用者应自行确认行为符合目标软件的用户协议、平台规则和所在地法律法规。因使用本项目导致的账号、数据、设备、第三方服务或其他损失，均由使用者自行承担。
