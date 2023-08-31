namespace Desmond;

internal class Const
{
    internal static string CWD => Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Common.Name),
        PlatformID.Unix => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Steam"),
        _ => throw new PlatformNotSupportedException()
    };

    #region SteamCMD
    internal const int AppID = 232130;
    internal const string Success = "Success!";

    internal static readonly string SteamCMD = Path.Combine(CWD, Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => "steamcmd.exe",
        PlatformID.Unix => "steamcmd.sh",
        _ => throw new PlatformNotSupportedException()
    });

    internal static readonly string URL = Environment.OSVersion.Platform switch
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
    internal static string KFServer => Path.Combine(CWD, Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => @"steamapps\common\kf2server\Binaries\Win64\KFServer.exe",
        PlatformID.Unix => "steamapps/common/kf2server/Binaries/Win64/KFGameSteamServer.bin.x86_64",
        _ => throw new PlatformNotSupportedException()
    });

    internal static string Process => Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => Path.GetFileNameWithoutExtension(KFServer),
        PlatformID.Unix => Path.GetFileName(KFServer),
        _ => throw new PlatformNotSupportedException()
    };

    internal static readonly string Logs = Path.Combine(CWD, Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => @"steamapps\common\kf2server\KFGame\Logs",
        _ => throw new PlatformNotSupportedException()
    });

    static readonly string Prefix = Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => "PC",
        PlatformID.Unix => "Linux",
        _ => throw new PlatformNotSupportedException()
    };

    internal static readonly string KFGame = Prefix + "Server-KFGame.ini";
    internal static readonly string KFEngine = Prefix + "Server-KFEngine.ini";
    internal const string KFWeb = "KFWeb.ini";
    internal const string GameInfo = "KFGame.KFGameInfo";
    internal const string EngineInfo = "Engine.GameInfo";
    internal const string MapCycles = "GameMapCycles";
    internal const string PublicIP = "Public IP";
    internal const string IntendedWeekly = "Intended weekly index:";
    internal const string UsedWeekly = "USED Weekly index:";
    internal const string Extension = "log";

    internal    static readonly string Config = Path.Combine(CWD, Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => @"steamapps\common\kf2server\KFGame\Config",
        _ => throw new PlatformNotSupportedException()
    });
    #endregion
}
