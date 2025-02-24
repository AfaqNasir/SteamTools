#if (WINDOWS || MACCATALYST || MACOS || LINUX) && !(IOS || ANDROID)
using AppResources = BD.WTTS.Client.Resources.Strings;
using ISteamConnectService = BD.SteamClient.Services.Mvvm.ISteamConnectService;
using SteamServiceBaseImpl = BD.SteamClient.Services.Implementation.SteamServiceImpl;

// ReSharper disable once CheckNamespace
namespace BD.WTTS.Services.Implementation;

sealed class SteamServiceImpl2 : SteamServiceBaseImpl, ISteamConnectService
{
    readonly IPlatformService platform;
    readonly IServiceProvider serviceProvider;

    public SteamServiceImpl2(
        IServiceProvider serviceProvider,
        IPlatformService platform,
        ILoggerFactory loggerFactory) : base(loggerFactory)
    {
        this.platform = platform;
        this.serviceProvider = serviceProvider;
    }

    bool ISteamConnectService.IsConnectToSteam
    {
        get => SteamConnectService.Current.IsConnectToSteam;
        set => SteamConnectService.Current.IsConnectToSteam = value;
    }

    SourceCache<SteamApp, uint> ISteamConnectService.SteamApps => SteamConnectService.Current.SteamApps;

    SourceCache<SteamApp, uint> ISteamConnectService.DownloadApps => SteamConnectService.Current.DownloadApps;

    SourceCache<SteamUser, long> ISteamConnectService.SteamUsers => SteamConnectService.Current.SteamUsers;

    SteamUser? ISteamConnectService.CurrentSteamUser => SteamConnectService.Current.CurrentSteamUser;

    public override ISteamConnectService Conn => this;

    protected override string? StratSteamDefaultParameter => SteamSettings.SteamStratParameter.Value;

    protected override bool IsRunSteamAdministrator => SteamSettings.IsRunSteamAdministrator.Value;

    protected override Dictionary<uint, string?>? HideGameList => serviceProvider.GetService<IGameLibrarySettings>()?.HideGameList;

    protected override string? GetString(string name) => AppResources.ResourceManager.GetString(name);

    protected override Process? StartAsInvoker(string fileName, string? arguments = null)
    {
        return platform.StartAsInvoker(fileName, arguments);
    }

    protected override void SetSteamCurrentUser(string userName)
    {
#if WINDOWS
        if (DesktopBridge.IsRunningAsUwp)
        {
            var reg = $"Windows Registry Editor Version 5.00{Environment.NewLine}[HKEY_CURRENT_USER\\Software\\Valve\\Steam]{Environment.NewLine}\"AutoLoginUser\"=\"{userName}\"";
            var path = IOPath.GetCacheFilePath(WindowsPlatformServiceImpl.CacheTempDirName, "SwitchSteamUser", FileEx.Reg);
            File.WriteAllText(path, reg, Encoding.UTF8);
            var p = WindowsPlatformServiceImpl.StartProcessRegedit($"/s \"{path}\"");
            IOPath.TryDeleteInDelay(p, path);
            return;
        }
#endif
        base.SetSteamCurrentUser(userName);
    }
}
#endif