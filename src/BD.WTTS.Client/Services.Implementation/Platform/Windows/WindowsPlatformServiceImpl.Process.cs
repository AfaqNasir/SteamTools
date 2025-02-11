#if WINDOWS

// ReSharper disable once CheckNamespace
namespace BD.WTTS.Services.Implementation;

partial class WindowsPlatformServiceImpl
{
    public string GetCommandLineArgs(Process process)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                  "SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id);
            using var objects = searcher.Get();
            var @object = objects.Cast<ManagementBaseObject>().SingleOrDefault();
            return @object?["CommandLine"]?.ToString() ?? "";
        }
        catch (Win32Exception ex) when ((uint)ex.ErrorCode == 0x80004005)
        {
            // 没有对该进程的安全访问权限。
            return string.Empty;
        }
    }
}
#endif