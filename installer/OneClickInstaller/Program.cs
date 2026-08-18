using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace MechrevoCustomOSD.Installer;

internal static class Program
{
    private const string ProductName = "Mechrevo Custom OSD v1.0.0";
    private const string TemporaryPrefix = "MechrevoCustomOSD-v1.0.0-";

    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            MessageBox.Show(
                "This release has only been tested on Windows 11 x64 and will not install on an older Windows build.\n\n" +
                "此版本仅在 Windows 11 x64 上测试，不会安装到更早的 Windows 版本。",
                ProductName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return 2;
        }

        string installDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "MechrevoCustomOSD");
        bool isInstalled = File.Exists(Path.Combine(installDirectory, "app", "MechrevoCustomOSD.exe")) ||
                           File.Exists(Path.Combine(installDirectory, "original-state.json"));
        string action = args.Any(argument => string.Equals(argument, "--uninstall", StringComparison.OrdinalIgnoreCase))
            ? "Uninstall"
            : "Install";

        if (action == "Uninstall")
        {
            if (!isInstalled)
            {
                MessageBox.Show(
                    "Mechrevo Custom OSD is not installed.\n\n机械革命自定义 OSD 尚未安装。",
                    ProductName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return 1;
            }

            DialogResult removeConfirmation = MessageBox.Show(
                "Uninstall Mechrevo Custom OSD and restore the recorded OEM service state?\n\n" +
                "是否卸载机械革命自定义 OSD，并恢复安装时记录的官方服务状态？",
                ProductName,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (removeConfirmation != DialogResult.Yes) return 1;
        }
        else if (isInstalled)
        {
            DialogResult existingChoice = MessageBox.Show(
                "Mechrevo Custom OSD is already installed.\n\n" +
                "Yes: update or repair the installation\nNo: uninstall and restore the OEM service\nCancel: make no changes\n\n" +
                "机械革命自定义 OSD 已安装。\n\n" +
                "是：更新或修复安装\n否：卸载并恢复官方服务\n取消：不作更改",
                ProductName,
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button3);
            if (existingChoice == DialogResult.Cancel) return 1;
            action = existingChoice == DialogResult.No ? "Uninstall" : "Install";
        }
        else
        {
            DialogResult installConfirmation = MessageBox.Show(
                "Install Mechrevo Custom OSD for the WUJIE 14 Pro?\n\n" +
                "The installer will save and disable the OEM BLDHotKeyService, then create a standard-user logon task. " +
                "This prevents the OEM and custom OSDs from conflicting. The OEM files are not deleted and the service state can be restored by uninstalling.\n\n" +
                "是否安装机械革命无界14 Pro 自定义 OSD？\n\n" +
                "安装器会保存并停用官方 BLDHotKeyService，然后创建普通用户登录任务，以避免两个 OSD 冲突。官方文件不会被删除，卸载时会恢复服务状态。",
                ProductName,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (installConfirmation != DialogResult.Yes) return 1;
        }

        string? systemCommandHost = FindSystemCommandHost();
        if (systemCommandHost is null)
        {
            MessageBox.Show(
                "A required Windows system component could not be found. Repair Windows system components, then run this installer again.\n\n" +
                "未找到必需的 Windows 系统组件。请修复 Windows 系统组件后再运行安装器。",
                ProductName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return 3;
        }

        string temporaryDirectory = Path.Combine(Path.GetTempPath(), TemporaryPrefix + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(temporaryDirectory);
            ExtractPayload(temporaryDirectory);

            string setupScript = Path.Combine(temporaryDirectory, "Setup-MechrevoCustomOSD.ps1");
            if (!File.Exists(setupScript)) throw new FileNotFoundException("The embedded setup script is missing.", setupScript);

            ProcessStartInfo startInfo = new()
            {
                FileName = systemCommandHost,
                WorkingDirectory = temporaryDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            startInfo.ArgumentList.Add("-NoLogo");
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-ExecutionPolicy");
            startInfo.ArgumentList.Add("Bypass");
            startInfo.ArgumentList.Add("-File");
            startInfo.ArgumentList.Add(setupScript);
            startInfo.ArgumentList.Add("-Action");
            startInfo.ArgumentList.Add(action);

            using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start the Windows setup host.");
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            Task.WaitAll(outputTask, errorTask);

            if (process.ExitCode != 0)
            {
                string details = string.Join(Environment.NewLine, outputTask.Result, errorTask.Result).Trim();
                MessageBox.Show(
                    (action == "Install" ? "Installation failed.\n\n安装失败。\n\n" : "Uninstall failed.\n\n卸载失败。\n\n") + Limit(details, 3500),
                    ProductName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return process.ExitCode;
            }

            string completionMessage = action == "Install"
                ? "Installation completed. Use the tray icon to preview the OSD, change language, or exit.\n\n" +
                  "安装完成。可通过托盘图标测试 OSD、切换语言或退出。"
                : "Uninstall completed. The recorded OEM service state has been restored.\n\n" +
                  "卸载完成，已恢复安装时记录的官方服务状态。";
            MessageBox.Show(
                completionMessage,
                ProductName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return 0;
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                (action == "Install" ? "Installation failed.\n\n安装失败。\n\n" : "Uninstall failed.\n\n卸载失败。\n\n") + Limit(exception.ToString(), 3500),
                ProductName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return 10;
        }
        finally
        {
            TryDeleteTemporaryDirectory(temporaryDirectory);
        }
    }

    private static string? FindSystemCommandHost()
    {
        string systemDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System);
        if (string.IsNullOrWhiteSpace(systemDirectory)) return null;
        string builtIn = Path.Combine(systemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");
        return File.Exists(builtIn) ? builtIn : null;
    }

    private static void ExtractPayload(string destination)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        using Stream payload = assembly.GetManifestResourceStream("MechrevoCustomOSD.payload.zip")
            ?? throw new InvalidOperationException("The embedded installer payload is missing.");
        using ZipArchive archive = new(payload, ZipArchiveMode.Read, false);

        string root = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            string normalizedName = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
            string target = Path.GetFullPath(Path.Combine(root, normalizedName));
            if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The installer payload contains an unsafe path.");
            }

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(target);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            using Stream input = entry.Open();
            using FileStream output = new(target, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            input.CopyTo(output);
        }
    }

    private static void TryDeleteTemporaryDirectory(string path)
    {
        try
        {
            string fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
            string tempRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar);
            string? parent = Path.GetDirectoryName(fullPath);
            string leaf = Path.GetFileName(fullPath);
            if (string.Equals(parent, tempRoot, StringComparison.OrdinalIgnoreCase) &&
                leaf.StartsWith(TemporaryPrefix, StringComparison.Ordinal))
            {
                Directory.Delete(fullPath, true);
            }
        }
        catch
        {
        }
    }

    private static string Limit(string text, int maximum)
    {
        if (string.IsNullOrWhiteSpace(text)) return "No additional details were returned.";
        return text.Length <= maximum ? text : text[..maximum] + Environment.NewLine + "…";
    }
}
