using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Skia;
using FluentAvalonia.Styling;
using System.Application.Models;
using System.Application.Mvvm;
using System.Application.Security;
using System.Application.Services;
using System.Application.Services.Implementation;
using System.Application.Settings;
using System.Application.UI.Resx;
using System.Application.UI.ViewModels;
using System.Application.UI.Views;
using System.Application.UI.Views.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Properties;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using AvaloniaApplication = Avalonia.Application;
using ShutdownMode = Avalonia.Controls.ShutdownMode;
using Window = Avalonia.Controls.Window;
using WindowState = Avalonia.Controls.WindowState;
using Avalonia.Markup.Xaml.MarkupExtensions;
#if WINDOWS
//using WpfApplication = System.Windows.Application;
#endif

[assembly: Guid("82cda250-48a2-48ad-ab03-5cda873ef80c")]

namespace System.Application.UI
{
    public partial class App : AvaloniaApplication, IDisposableHolder, IApplication, IAvaloniaApplication, IClipboardPlatformService
    {
        public static App Instance => Current is App app ? app : throw new ArgumentNullException(nameof(app));

        #region IApplication.IDesktopProgramHost

        public IApplication.IDesktopProgramHost ProgramHost { get; }

        IApplication.IProgramHost IApplication.ProgramHost => ProgramHost;

        #endregion

        public App() : this(null!) { }

        public App(IApplication.IDesktopProgramHost host)
        {
            ProgramHost = host;
        }

        const AppTheme _DefaultActualTheme = AppTheme.Dark;

        AppTheme IApplication.DefaultActualTheme => _DefaultActualTheme;

        AppTheme mTheme = _DefaultActualTheme;

        public AppTheme Theme
        {
            get => mTheme;
            set
            {
                if (value == mTheme) return;
                AppTheme switch_value = value;

                if (value == AppTheme.FollowingSystem)
                {
                    var dps = IPlatformService.Instance;
                    var isLightOrDarkTheme = dps.IsLightOrDarkTheme;
                    if (isLightOrDarkTheme.HasValue)
                    {
                        switch_value = IApplication.GetAppThemeByIsLightOrDarkTheme(isLightOrDarkTheme.Value);
                        dps.SetLightOrDarkThemeFollowingSystem(true);
                        if (switch_value == mTheme) goto setValue;
                    }
                }
                else if (mTheme == AppTheme.FollowingSystem)
                {
                    var dps = IPlatformService.Instance;
                    dps.SetLightOrDarkThemeFollowingSystem(false);
                    var isLightOrDarkTheme = dps.IsLightOrDarkTheme;
                    if (isLightOrDarkTheme.HasValue)
                    {
                        var mThemeFS = IApplication.GetAppThemeByIsLightOrDarkTheme(isLightOrDarkTheme.Value);
                        if (mThemeFS == switch_value) goto setValue;
                    }
                }

                SetThemeNotChangeValue(switch_value);

            setValue: mTheme = value;
            }
        }

        public void SetThemeNotChangeValue(AppTheme value)
        {
            string? themeName = null;
            //FluentThemeMode mode;

            switch (value)
            {
                case AppTheme.HighContrast:
                    themeName = FluentAvaloniaTheme.HighContrastModeString;
                    //mode = FluentThemeMode.Light;
                    break;
                case AppTheme.Light:
                    themeName = FluentAvaloniaTheme.LightModeString;
                    //mode = FluentThemeMode.Light;
                    //LiveCharts.CurrentSettings.AddLightTheme();
                    break;
                case AppTheme.Dark:
                default:
                    themeName = FluentAvaloniaTheme.DarkModeString;
                    //mode = FluentThemeMode.Dark;
                    //LiveCharts.CurrentSettings.AddDarkTheme();
                    break;
            }

            //var uri_0 = new Uri($"avares://Avalonia.Themes.Fluent/Fluent{themeName}.xaml");
            //var uri_1 = new Uri($"avares://System.Application.SteamTools.Client.Avalonia/Application/UI/Styles/Theme{themeName}.xaml");

            var faTheme = AvaloniaLocator.Current.GetService<FluentAvaloniaTheme>();
            if (faTheme != null)
                faTheme.RequestedTheme = themeName;

            var csTheme = AvaloniaLocator.Current.GetService<CustomTheme>();
            if (csTheme != null)
                csTheme.Mode = themeName;

            //if (Resources.Count > 1)
            //{
            //    //Styles[0] = new FluentTheme(uri_0)
            //    //{
            //    //    Mode = mode,
            //    //};
            //    Resources.MergedDictionaries[0] = (ResourceDictionary)AvaloniaXamlLoader.Load(uri_1);
            //}
        }

        public static void SetThemeAccent(string? colorHex)
        {
            if (colorHex == null)
            {
                return;
            }
            var thm = AvaloniaLocator.Current.GetService<FluentAvaloniaTheme>()!;

            if (Color.TryParse(colorHex, out var color))
            {
                thm.CustomAccentColor = color;
            }
            else
            {
                if (colorHex.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase))
                {
                    thm.CustomAccentColor = null;
                }
            }

            if (OperatingSystem2.IsWindows())
            {
                if (OperatingSystem2.IsWindowsVersionAtLeast(6, 2))
                    thm.PreferUserAccentColor = true;
                else
                    thm.PreferUserAccentColor = false;
            }
            else
            {
                thm.PreferUserAccentColor = true;
            }
        }

        readonly Dictionary<string, ICommand> mNotifyIconMenus = new();

        public IReadOnlyDictionary<string, ICommand> NotifyIconMenus => mNotifyIconMenus;

        public override void Initialize()
        {
            const bool isTrace =
#if StartWatchTrace
                true;
#else
                false;
#endif
#if StartWatchTrace
            /*if (isTrace) */StartWatchTrace.Record("App.Initialize");
#endif
            AvaloniaXamlLoader.Load(this);
#if StartWatchTrace
            /*if (isTrace) */StartWatchTrace.Record("App.LoadXaml");
#endif
            Name = ThisAssembly.AssemblyTrademark;
            ProgramHost.OnCreateAppExecuted(handlerViewModelManager: vmService =>
            {
                switch (vmService.MainWindow)
                {
                    case CloudArchiveWindowViewModel:
                        ProgramHost.IsMinimize = false;
                        MainWindow = new CloudArchiveWindow();
                        break;

                    case AchievementWindowViewModel:
                        ProgramHost.IsMinimize = false;
                        MainWindow = new AchievementWindow();
                        break;

                    default:
                        #region 主窗口启动时加载的资源
#if !UI_DEMO
                        compositeDisposable.Add(SettingsHost.Save);
                        compositeDisposable.Add(ProxyService.Current.Exit);
                        compositeDisposable.Add(SteamConnectService.Current.Dispose);
                        compositeDisposable.Add(ASFService.Current.StopASF);
#pragma warning disable CA1416 // 验证平台兼容性
                        if (GeneralSettings.IsStartupAppMinimized.Value)
                            ProgramHost.IsMinimize = true;
#pragma warning restore CA1416 // 验证平台兼容性
#endif
                        #endregion
                        MainWindow = new MainWindow();
                        break;
                }
                MainWindow.DataContext = vmService.MainWindow;
            }, isTrace: isTrace);

            LiveCharts.Configure(config =>
            {
                config
                    // registers SkiaSharp as the library backend
                    // REQUIRED unless you build your own
                    .AddSkiaSharp();
                // adds the default supported types
                // OPTIONAL but highly recommend
                //.AddDefaultMappers()

                // select a theme, default is Light
                // OPTIONAL
                //.AddDarkTheme()

                if (Theme != AppTheme.FollowingSystem)
                {
                    if (Theme == AppTheme.Light)
                        config.AddLightTheme();
                    else
                        config.AddDarkTheme();
                }
                else
                {
                    var dps = IPlatformService.Instance;
                    dps.SetLightOrDarkThemeFollowingSystem(false);
                    var isLightOrDarkTheme = dps.IsLightOrDarkTheme;
                    if (isLightOrDarkTheme.HasValue)
                    {
                        var mThemeFS = IApplication.GetAppThemeByIsLightOrDarkTheme(isLightOrDarkTheme.Value);
                        if (mThemeFS == AppTheme.Light)
                            config.AddLightTheme();
                        else
                            config.AddDarkTheme();
                    }
                }
            });
#if WINDOWS
            //InitWebView2();
#endif
        }

        //public ContextMenu? NotifyIconContextMenu { get; private set; }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ProgramHost.IsTrayProcess)
            {
                base.OnFrameworkInitializationCompleted();
                return;
            }

            // 在UI预览中，ApplicationLifetime 为 null
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                //#if MAC
                //                AppDelegate.Init();
                //#endif
                InitTrayIcon(desktop);

                desktop.MainWindow =
#if !UI_DEMO
                    ProgramHost.IsMinimize ? null :
#endif
                    MainWindow;

                desktop.Startup += Desktop_Startup;
                desktop.Exit += ApplicationLifetime_Exit;
            }

            base.OnFrameworkInitializationCompleted();
        }

        void InitTrayIcon(IClassicDesktopStyleApplicationLifetime? desktop)
        {
            if (desktop == null || IViewModelManager.Instance.MainWindow == null)
                return;
            if (ProgramHost.IsMainProcess)
            {
#pragma warning disable CA1416 // 验证平台兼容性
                StartupOptions.Value.HasNotifyIcon = GeneralSettings.IsEnableTrayIcon.Value;
#pragma warning disable CA1416 // 验证平台兼容性

                if (StartupOptions.Value.HasNotifyIcon)
                {
                    IViewModelManager.Instance.InitTaskBarWindowViewModel();

                    NotifyIconHelper.Init(this, NotifyIcon_Click);
                    //                        if (!OperatingSystem2.IsLinux())
                    //                        {
                    //                            (var notifyIcon, var menuItemDisposable) = NotifyIconHelper.Init(NotifyIconHelper.GetIconByCurrentAvaloniaLocator);
                    //                            notifyIcon.Click += NotifyIcon_Click;
                    //                            notifyIcon.DoubleClick += NotifyIcon_Click;
                    //                            if (menuItemDisposable != null) menuItemDisposable.AddTo(this);
                    //                            notifyIcon.AddTo(this);
                    //                        }
                    //                        else
                    //                        {
                    //#if LINUX || DEBUG
                    //                            NotifyIconHelper.StartPipeServer();
                    //#endif
                    //                        }
                }
                else
                {
                    NotifyIconHelper.Dispoe();
                    IViewModelManager.Instance.DispoeTaskBarWindowViewModel();
                }

                desktop.ShutdownMode =
#if UI_DEMO
                    ShutdownMode.OnMainWindowClose;
#else
                StartupOptions.Value.HasNotifyIcon ? ShutdownMode.OnExplicitShutdown : ShutdownMode.OnMainWindowClose;
#endif

            }
        }

        /// <summary>
        /// 初始化设置项变更时监听
        /// </summary>
        void IApplication.InitSettingSubscribe()
        {
            UISettings.Theme.Subscribe(x => Theme = (AppTheme)x);
            UISettings.Language.Subscribe(R.ChangeLanguage);

            if (IApplication.IsDesktopPlatform)
            {
                GeneralSettings.IsEnableTrayIcon.Subscribe(x => InitTrayIcon(ApplicationLifetime as IClassicDesktopStyleApplicationLifetime));
            }
        }

        /// <summary>
        /// override RegisterServices register custom service
        /// </summary>
        public override void RegisterServices()
        {
            IViewModelBase.IsInDesignMode = Design.IsDesignMode;
            if (ViewModelBase.IsInDesignMode)
                ProgramHost.ConfigureServices(DILevel.MainProcess | DILevel.GUI);

            AvaloniaLocator.CurrentMutable.Bind<IFontManagerImpl>().ToConstant(DI.Get<IFontManagerImpl>());

            //#if WINDOWS
            //            if (!ViewModelBase.IsInDesignMode && OperatingSystem2.IsWindows10AtLeast)
            //            {
            //                AvaloniaLocator.CurrentMutable.Bind<IWindowingPlatform>().ToConstant(new AvaloniaWin32WindowingPlatformImpl());
            //            }
            //#endif

            AvaloniaLocator.CurrentMutable.Bind<IPlatformRenderInterface>().ToConstant(SkiaPlatform2.PlatformRenderInterface);

            base.RegisterServices();
        }

        /// <inheritdoc cref="IApplication.InitSettingSubscribe"/>
        void PlatformInitSettingSubscribe()
        {
            ((IApplication)this).InitSettingSubscribe();
#pragma warning disable CA1416 // 验证平台兼容性
            UISettings.ThemeAccent.Subscribe(SetThemeAccent);
            UISettings.GetUserThemeAccent.Subscribe(x => SetThemeAccent(x ? bool.TrueString : UISettings.ThemeAccent.Value));

            GeneralSettings.WindowsStartupAutoRun.Subscribe(IApplication.SetBootAutoStart);

            UISettings.WindowBackgroundMateria.Subscribe(SetAllWindowransparencyMateria, false);

            if (OperatingSystem2.IsWindows())
            {
                UISettings.EnableDesktopBackground.Subscribe(x =>
                {
                    if (x)
                    {
                        //var t = (WindowTransparencyLevel)UISettings.WindowBackgroundMateria.Value;
                        //if (t == WindowTransparencyLevel.None ||
                        //    t == WindowTransparencyLevel.Mica)
                        //{
                        //    UISettings.EnableDesktopBackground.Value = false;
                        //    Toast.Show(string.Format(AppResources.Settings_UI_EnableDesktopBackground_Error, t));
                        //    return;
                        //}
                        SetDesktopBackgroundWindow();
                    }
                }, false);
            }
#pragma warning restore CA1416 // 验证平台兼容性
        }

        void IApplication.PlatformInitSettingSubscribe() => PlatformInitSettingSubscribe();

        void Desktop_Startup(object? sender, ControlledApplicationLifetimeStartupEventArgs e)
        {
            //            var isOfficialChannelPackage = IsNotOfficialChannelPackageDetectionHelper.Check(ProgramHost.IsMainProcess);

            //#if StartWatchTrace
            //            StartWatchTrace.Record("Desktop_Startup.Start");
            //#endif
            //            IsNotOfficialChannelPackageDetectionHelper.Check();
            //#if WINDOWS || XAMARIN_MAC
            //            if (isOfficialChannelPackage)
            //            {
            //#pragma warning disable CA1416 // 验证平台兼容性
            //                ProgramHost.InitVisualStudioAppCenterSDK();
            //#pragma warning restore CA1416 // 验证平台兼容性
            //            }
            //#endif
            //#if StartWatchTrace
            //            StartWatchTrace.Record("AppCenterSDK.Init");
            //#endif
            //            AppHelper.Initialized?.Invoke();
            //#if StartWatchTrace
            //            StartWatchTrace.Record("Desktop_Startup.AppHelper.Initialized?");
            //#endif
            ProgramHost.OnStartup();
            //#if StartWatchTrace
            //            if (Program.IsMainProcess)
            //            {
            //                StartWatchTrace.Record("Desktop_Startup.MainProcess");
            //            }
            //#endif

            //            StartupToastIntercept.OnStartuped();
            //#if StartWatchTrace
            //            StartWatchTrace.Record("Desktop_Startup.SetIsStartuped");
            //#endif
            //            if (ProgramHost.IsMainProcess)
            //            {
            //                INotificationService.ILifeCycle.Instance?.OnStartup();
            //            }
        }

        void ApplicationLifetime_Exit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            //#if LINUX || DEBUG
            //            NotifyIconHelper.StopPipeServer();
            //#endif

            try
            {
                compositeDisposable.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error("Shutdown", ex, "compositeDisposable.Dispose()");
            }
#if WINDOWS
            //WpfApplication.Current.Shutdown();
#endif
            //AppHelper.TryShutdown();

            try
            {
                // https://appcenter.ms/orgs/BeyondDimension/apps/Steam/crashes/errors/952960442u/overview
                INotificationService.ILifeCycle.Instance?.OnShutdown();
            }
            catch (ObjectDisposedException)
            {

            }
        }

        internal void NotifyIcon_Click(object? sender, EventArgs e)
        {
            RestoreMainWindow();
        }

        Task IClipboardPlatformService.PlatformSetTextAsync(string text) => Clipboard?.SetTextAsync(text) ?? Task.CompletedTask;

        Task<string> IClipboardPlatformService.PlatformGetTextAsync() => Clipboard?.GetTextAsync() ?? Task.FromResult(string.Empty);

        bool IClipboardPlatformService.PlatformHasText
        {
            get
            {
                Func<Task<string>> func = () => Clipboard?.GetTextAsync() ?? Task.FromResult(string.Empty);
                var value = func.RunSync();
                return !string.IsNullOrEmpty(value);
            }
        }

        public Window? MainWindow { get; set; }

        AvaloniaApplication IAvaloniaApplication.Current => Current!;

        /// <summary>
        /// Restores the app's main window by setting its <c>WindowState</c> to
        /// <c>WindowState.Normal</c> and showing the window.
        /// </summary>
        public void RestoreMainWindow()
        {
            Window? mainWindow = null;

        ReTry:
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                mainWindow = desktop.MainWindow;
                if (mainWindow == null)
                {
                    //mainWindow = MainWindow;
                    //desktop.MainWindow = MainWindow;
                    mainWindow = MainWindow = desktop.MainWindow = new MainWindow();
                    mainWindow.DataContext = IViewModelManager.Instance.MainWindow;
                }

            }

            if (mainWindow == null)
            {
                throw new ArgumentNullException(nameof(mainWindow));
            }

            try
            {
                mainWindow.Show();
            }
            catch (InvalidOperationException)
            {
                mainWindow = null;
                goto ReTry;
            }

            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Topmost = true;
            mainWindow.Topmost = false;
            mainWindow.BringIntoView();
            mainWindow.ActivateWorkaround(); // Extension method hack because of https://github.com/AvaloniaUI/Avalonia/issues/2975
            mainWindow.Focus();

            //// Again, ugly hack because of https://github.com/AvaloniaUI/Avalonia/issues/2994
            //mainWindow.Width += 0.1;
            //mainWindow.Width -= 0.1;
        }

        public void SetTopmostOneTime()
        {
            if (MainWindow != null && MainWindow.WindowState != WindowState.Minimized)
            {
                MainWindow.Topmost = true;
                MainWindow.Topmost = false;
            }
        }

        public bool HasActiveWindow()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.Windows.Any_Nullable(x => x.IsActive))
                {
                    return true;
                }
            }
            return false;
        }

        public Window GetActiveWindow()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var activeWindow = desktop.Windows.FirstOrDefault(x => x.IsActive);
                if (activeWindow != null)
                {
                    return activeWindow;
                }
            }
            return MainWindow!;
        }

        public void SetDesktopBackgroundWindow()
        {
            if (OperatingSystem2.IsWindows() && MainWindow is MainWindow window)
            {
#pragma warning disable CA1416 // 验证平台兼容性
                INativeWindowApiService.Instance!.SetDesktopBackgroundToWindow(window.BackHandle, Convert.ToInt32(window.Width), Convert.ToInt32(window.Height));
#pragma warning restore CA1416 // 验证平台兼容性
            }
        }

        /// <summary>
        /// 设置当前打开窗口的 AvaloniaWindow 背景透明材质
        /// </summary>
        /// <param name="level"></param>
        public void SetAllWindowransparencyMateria(int level)
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                foreach (var window in desktop.Windows)
                {
                    window.TransparencyLevelHint = (WindowTransparencyLevel)level;

                    if (window.TransparencyLevelHint == WindowTransparencyLevel.Transparent ||
                        window.TransparencyLevelHint == WindowTransparencyLevel.Blur)
                    {
                        ((IPseudoClasses)window.Classes).Set(":transparent", true);
                    }
                    else
                    {
                        ((IPseudoClasses)window.Classes).Set(":transparent", false);
                    }
                }
            }
        }

        /// <summary>
        /// Exits the app by calling <c>Shutdown()</c> on the <c>IClassicDesktopStyleApplicationLifetime</c>.
        /// </summary>
        public bool Shutdown(int exitCode = 0)
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                MainThread2.BeginInvokeOnMainThread(() =>
                {
                    desktop.Shutdown(exitCode);
                });
                return true;
            }
            return false;
        }

        void IApplication.Shutdown() => Shutdown();

        //bool IDesktopAppService.IsCefInitComplete => false;
        //CefNetApp.InitState == CefNetAppInitState.Complete;

        #region IDisposable members

        readonly CompositeDisposable compositeDisposable = new();

        CompositeDisposable IApplication.CompositeDisposable => compositeDisposable;

        ICollection<IDisposable> IDisposableHolder.CompositeDisposable => compositeDisposable;

        void IDisposable.Dispose()
        {
            compositeDisposable.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion

        string IAvaloniaApplication.RenderingSubsystemName => "Skia";

        object IApplication.CurrentPlatformUIHost => MainWindow!;

        DeploymentMode IApplication.DeploymentMode => ProgramHost.DeploymentMode;
    }
}