[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Install', 'Uninstall', 'Status', 'Test')]
    [string]$Action
)

$ErrorActionPreference = 'Stop'
$utf8 = New-Object System.Text.UTF8Encoding($false)
[Console]::OutputEncoding = $utf8
$OutputEncoding = $utf8
$taskName = 'Mechrevo Custom OSD'
$legacyTaskName = 'Mechrevo OSD Topmost Workaround'
$officialServiceName = 'BLDHotKeyService'
$officialProcessName = 'BLDFnHotkeyUtility'
$installDirectory = Join-Path $env:ProgramData 'MechrevoCustomOSD'
$installedAppDirectory = Join-Path $installDirectory 'app'
$installedExecutable = Join-Path $installedAppDirectory 'MechrevoCustomOSD.exe'
$installedSetupScript = Join-Path $installDirectory 'Setup-MechrevoCustomOSD.ps1'
$legacyInstalledExecutable = Join-Path $installDirectory 'MechrevoCustomOSD.exe'
$configurationPath = Join-Path $installDirectory 'original-state.json'
$sourceAppDirectory = Join-Path $PSScriptRoot 'app'
$sourceExecutable = Join-Path $sourceAppDirectory 'MechrevoCustomOSD.exe'

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Invoke-SelfElevated {
    $hostPath = (Get-Process -Id $PID).Path
    $arguments = @(
        '-NoLogo'
        '-NoProfile'
        '-ExecutionPolicy'
        'Bypass'
        '-File'
        ('"{0}"' -f $PSCommandPath)
        '-Action'
        $Action
    )
    $process = Start-Process -FilePath $hostPath -ArgumentList $arguments -Verb RunAs -Wait -PassThru
    exit $process.ExitCode
}

function Test-ExactDirectory([string]$Path, [string]$ExpectedLeaf) {
    $programData = [IO.Path]::GetFullPath($env:ProgramData).TrimEnd('\')
    $resolved = [IO.Path]::GetFullPath($Path).TrimEnd('\')
    $expected = [IO.Path]::GetFullPath((Join-Path $programData $ExpectedLeaf)).TrimEnd('\')
    return $resolved -eq $expected
}

function Stop-CustomOSD {
    foreach ($candidate in @($installedExecutable, $legacyInstalledExecutable)) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            Start-Process -FilePath $candidate -ArgumentList '--stop' -Wait -WindowStyle Hidden
        }
    }
    Start-Sleep -Milliseconds 500
    Get-Process -Name 'MechrevoCustomOSD' -ErrorAction SilentlyContinue | Stop-Process -Force
}

function Remove-CustomTask {
    $task = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
    if ($null -ne $task) {
        Stop-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
        Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
    }
}

function Remove-LegacyWorkaround {
    $legacyTask = Get-ScheduledTask -TaskName $legacyTaskName -ErrorAction SilentlyContinue
    if ($null -ne $legacyTask) {
        Stop-ScheduledTask -TaskName $legacyTaskName -ErrorAction SilentlyContinue
        Unregister-ScheduledTask -TaskName $legacyTaskName -Confirm:$false
    }

    $legacyDirectory = Join-Path $env:ProgramData 'MechrevoOSDTopmost'
    if ((Test-ExactDirectory -Path $legacyDirectory -ExpectedLeaf 'MechrevoOSDTopmost') -and
        (Test-Path -LiteralPath $legacyDirectory)) {
        Remove-Item -LiteralPath $legacyDirectory -Recurse -Force
    }
}

function Get-ServiceSnapshot {
    $service = Get-CimInstance Win32_Service -Filter "Name='$officialServiceName'" -ErrorAction SilentlyContinue
    if ($null -eq $service) {
        return [ordered]@{ ServiceExisted = $false; StartMode = $null; WasRunning = $false }
    }
    return [ordered]@{
        ServiceExisted = $true
        StartMode = [string]$service.StartMode
        WasRunning = ([string]$service.State -eq 'Running')
    }
}

function Restore-OfficialService {
    $state = $null
    if (Test-Path -LiteralPath $configurationPath -PathType Leaf) {
        $state = Get-Content -LiteralPath $configurationPath -Raw | ConvertFrom-Json
    }

    $service = Get-Service -Name $officialServiceName -ErrorAction SilentlyContinue
    if ($null -eq $service) { return }

    $startupType = 'Automatic'
    $shouldStart = $true
    if ($null -ne $state -and $state.ServiceExisted) {
        if ($state.StartMode -eq 'Manual') { $startupType = 'Manual' }
        elseif ($state.StartMode -eq 'Disabled') { $startupType = 'Disabled' }
        $shouldStart = [bool]$state.WasRunning
    }

    Set-Service -Name $officialServiceName -StartupType $startupType
    if ($shouldStart -and $startupType -ne 'Disabled') {
        Start-Service -Name $officialServiceName
    }
}

if ($Action -eq 'Status') {
    $task = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
    $service = Get-CimInstance Win32_Service -Filter "Name='$officialServiceName'" -ErrorAction SilentlyContinue
    $customProcess = Get-Process -Name 'MechrevoCustomOSD' -ErrorAction SilentlyContinue
    Write-Host ('WinUI 3 OSD：{0}' -f $(if ($null -eq $task) { '未安装' } else { "已安装，任务状态 $($task.State)，运行级别 $($task.Principal.RunLevel)" }))
    Write-Host ('自定义进程：{0}' -f $(if ($null -eq $customProcess) { '未运行' } else { "运行中，PID $($customProcess.Id -join ', ')" }))
    if ($null -ne $service) {
        Write-Host ('官方服务：状态 {0}，启动类型 {1}' -f $service.State, $service.StartMode)
    }
    exit 0
}

if ($Action -eq 'Test') {
    if (-not (Test-Path -LiteralPath $installedExecutable -PathType Leaf)) {
        throw 'WinUI 3 OSD 尚未安装。'
    }
    Start-Process -FilePath $installedExecutable -ArgumentList '--demo' -Wait -WindowStyle Hidden
    exit 0
}

if (-not (Test-IsAdministrator)) {
    Invoke-SelfElevated
}

if ($Action -eq 'Install') {
    if (-not (Test-Path -LiteralPath $sourceExecutable -PathType Leaf)) {
        throw "发布文件不完整：找不到 $sourceExecutable"
    }
    Stop-CustomOSD
    Remove-CustomTask
    New-Item -ItemType Directory -Path $installDirectory -Force | Out-Null

    if (-not (Test-Path -LiteralPath $configurationPath -PathType Leaf)) {
        $snapshot = Get-ServiceSnapshot
        $snapshot['CapturedAt'] = (Get-Date).ToString('o')
        $snapshot | ConvertTo-Json | Set-Content -LiteralPath $configurationPath -Encoding utf8
    }

    if (Test-Path -LiteralPath $installedAppDirectory) {
        $resolvedAppDirectory = [IO.Path]::GetFullPath($installedAppDirectory).TrimEnd('\')
        $expectedAppDirectory = [IO.Path]::GetFullPath((Join-Path $installDirectory 'app')).TrimEnd('\')
        if ($resolvedAppDirectory -ne $expectedAppDirectory) {
            throw '拒绝更新：应用目录未通过安全校验。'
        }
        Remove-Item -LiteralPath $installedAppDirectory -Recurse -Force
    }
    if (Test-Path -LiteralPath $legacyInstalledExecutable -PathType Leaf) {
        Remove-Item -LiteralPath $legacyInstalledExecutable -Force
    }
    Copy-Item -LiteralPath $sourceAppDirectory -Destination $installedAppDirectory -Recurse
    Copy-Item -LiteralPath $PSCommandPath -Destination $installedSetupScript -Force
    Remove-LegacyWorkaround

    $officialService = Get-Service -Name $officialServiceName -ErrorAction SilentlyContinue
    if ($null -ne $officialService) {
        Stop-Service -Name $officialServiceName -Force -ErrorAction SilentlyContinue
        Set-Service -Name $officialServiceName -StartupType Disabled
    }
    Get-Process -Name $officialProcessName -ErrorAction SilentlyContinue | Stop-Process -Force

    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent().Name
    $taskAction = New-ScheduledTaskAction -Execute $installedExecutable
    $taskTrigger = New-ScheduledTaskTrigger -AtLogOn -User $currentUser
    $taskPrincipal = New-ScheduledTaskPrincipal -UserId $currentUser -LogonType Interactive -RunLevel Limited
    $taskSettings = New-ScheduledTaskSettingsSet `
        -AllowStartIfOnBatteries `
        -DontStopIfGoingOnBatteries `
        -Hidden `
        -MultipleInstances IgnoreNew `
        -StartWhenAvailable

    Register-ScheduledTask `
        -TaskName $taskName `
        -Action $taskAction `
        -Trigger $taskTrigger `
        -Principal $taskPrincipal `
        -Settings $taskSettings `
        -Description 'Standard-user WinUI 3 Desktop Acrylic OSD using MECHREVO HID_EVENT20 firmware events.' `
        -Force | Out-Null

    Start-ScheduledTask -TaskName $taskName
    Start-Sleep -Milliseconds 1200
    Start-Process -FilePath $installedExecutable -ArgumentList '--demo' -Wait -WindowStyle Hidden
    Start-Sleep -Milliseconds 300

    $process = Get-Process -Name 'MechrevoCustomOSD' -ErrorAction SilentlyContinue
    if ($null -eq $process) {
        throw '安装完成，但 OSD 进程没有保持运行。请检查用户日志。'
    }
    Write-Host ('安装完成；普通权限 WinUI 3 OSD PID：{0}' -f ($process.Id -join ', '))
    Write-Host '官方 OSD 服务已停用但未删除；可从右下角托盘图标选择语言、测试或退出。'
    exit 0
}

if ($Action -eq 'Uninstall') {
    Stop-CustomOSD
    Remove-CustomTask
    Restore-OfficialService

    if (-not (Test-ExactDirectory -Path $installDirectory -ExpectedLeaf 'MechrevoCustomOSD')) {
        throw '拒绝删除：安装目录未通过安全校验。'
    }
    if (Test-Path -LiteralPath $installDirectory) {
        Remove-Item -LiteralPath $installDirectory -Recurse -Force
    }

    Write-Host '卸载完成；自定义任务和程序文件已移除，官方 OSD 服务状态已恢复。'
    Write-Host '语言设置和日志保留在当前用户 LocalAppData，便于重新安装；可手动删除。'
}
