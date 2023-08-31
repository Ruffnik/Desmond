namespace Desmond;

internal class Const
{
    #region General
    internal const string XML = "xml";

    internal static string CWD => Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Common.Name),
        PlatformID.Unix => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Steam"),
        _ => throw new PlatformNotSupportedException()
    };
    #endregion

    #region SteamCMD
    internal const int AppID = 232130;
    internal const string Success = "Success!";

    internal static string SteamCMD => Path.Combine(CWD, Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => "steamcmd.exe",
        PlatformID.Unix => "steamcmd.sh",
        _ => throw new PlatformNotSupportedException()
    });

    internal static string URL => Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip",
        PlatformID.Unix => "https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz",
        _ => throw new PlatformNotSupportedException()
    };

    internal static readonly string Dumps = Path.Combine(CWD, Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => "dumps",
        _ => throw new PlatformNotSupportedException()
    });
    #endregion

    #region KF2Server
    static string Binary => Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => "KFServer.exe",
        PlatformID.Unix => "KFGameSteamServer.bin.x86_64",
        _ => throw new PlatformNotSupportedException()
    };

    internal static string KFServer => Path.Combine(CWD, "steamapps", "common", "kf2server", "Binaries", "Win64", Binary);

    internal static string Process => Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => Path.GetFileNameWithoutExtension(Binary),
        PlatformID.Unix => Binary,
        _ => throw new PlatformNotSupportedException()
    };

    internal static string Logs => Path.Combine(CWD, "steamapps", "common", "kf2server", "KFGame", "Logs");

    static string Prefix => Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => "PC",
        PlatformID.Unix => "Linux",
        _ => throw new PlatformNotSupportedException()
    };

    internal static string KFGame => Prefix + "Server-KFGame.ini";
    internal static string KFEngine => Prefix + "Server-KFEngine.ini";
    internal const string KFWeb = "KFWeb.ini";
    internal const string GameInfo = "KFGame.KFGameInfo";
    internal const string EngineInfo = "Engine.GameInfo";
    internal const string MapCycles = "GameMapCycles";
    internal const string PublicIP = "Public IP";
    internal const string IntendedWeekly = "Intended weekly index:";
    internal const string UsedWeekly = "USED Weekly index:";
    internal const string Extension = "log";

    internal static string Config => Path.Combine(CWD, "steamapps", "common", "kf2server", "KFGame", "Config");
    #endregion
}