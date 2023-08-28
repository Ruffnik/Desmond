using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Runtime.Serialization;
using System.Text;

namespace Desmond;

[DataContract]
internal class KF2Server
{
    #region Static interface
    public static void TryUpdate()
    {
        while (!RunSteamCMD(AppID))
            KillAll();
    }

    public static void Clean()
    {
        Clean(Logs);
        Clean(Dumps);

        void Clean(string Folder)
        {
            if (Directory.Exists(Folder))
                Task.Run(() => new DirectoryInfo(Folder).GetFiles().ToList().ForEach(_ => Common.TryDelete(_.FullName)));
        }
    }

    public static void KillAll() => Process.GetProcessesByName(Path.GetFileNameWithoutExtension(KFServer)).ToList().ForEach(_ => _.Kill());

    public static (uint, IPAddress, IEnumerable<string>) GetStatus()
    {
        var Runner = new KF2Server(true);
        Runner.Run();
        Runner.Runner!.Kill();
        return (Runner.Weekly!.Value, Runner.Address!, Runner.Maps!);
    }
    #endregion

    #region KF2Server
    #region Configuration
#pragma warning disable 0649
    [DataMember]
    public bool? UsedForTakeover;
    [DataMember]
    public string? GamePassword, AdminPassword, ServerName, BannerLink, WebsiteLink;
    [DataMember]
    public IEnumerable<string>? ServerMOTD;
    [DataMember]
    public GameModes? GameMode;
    [DataMember]
    public Difficulties? Difficulty;
    [DataMember]
    public Lengths? GameLength;
#pragma warning restore 0649
    #endregion

    #region State
    const int Base = 7777 + 1;
    const int AdminBase = 8080 + 1;
    public int? Offset, OffsetWebAdmin;
    readonly bool ProbeMode;
    internal IPAddress? Address { get; private set; }
    internal IEnumerable<string>? Maps { get; private set; }
    internal uint? Weekly { get; private set; }

    internal string ConfigSubDir
    {
        private get => _ConfigSubDir!;
        set
        {
            _ConfigSubDir = Environment.OSVersion.Platform switch
            {
                PlatformID.Win32NT => value.Contains(' ') ? '"' + value + '"' : value,
                PlatformID.Unix => value.Replace(" ", "%20"),
                _ => throw new PlatformNotSupportedException()
            };
        }
    }

    internal bool Running => !Runner?.HasExited ?? false;

    internal void Wait() => Runner!.WaitForExit();

    public void Kill() => Runner!.Kill();
    #endregion

    #region Plumbing
    Process? Runner;
    string? _ConfigSubDir;

    public KF2Server(bool ProbeMode = false) => this.ProbeMode = ProbeMode;

    static string Find(string[] Lines, string Key) => Lines.Where(_ => _.Contains(Key)).First().Split(Key)[1].Trim().Split(' ')[0];
    #endregion

    public void Run(IEnumerable<string>? Maps = null)
    {
        string[] ContentKFGame = Array.Empty<string>();
        string[] ContentKFEngine = Array.Empty<string>();
        string[] ContentKFWeb = Array.Empty<string>();
        string[] Lines;
        string FileKFGame, FileKFEngine, FileKFWeb = string.Empty;

        if (!Running)
        {
            FileKFGame = Path.Combine(Config, KFGame);
            FileKFEngine = Path.Combine(Config, KFEngine);
            var Log = Path.ChangeExtension(Path.GetRandomFileName(), Extension);

            if (ProbeMode)
            {
                GamePassword = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
                if (TryReadINIsProbe())
                {
                    INI.TryRemove(ref ContentKFGame, GameInfo, MapCycles);
                    File.WriteAllLines(FileKFGame, ContentKFGame, Encoding.ASCII);
                }
                Runner = new() { StartInfo = new(KFServer, $"-log={Log}") };
            }
            else
            {
                this.Maps = Maps?.ToArray();
                var DirectoryConfig = Path.Combine(Config, ConfigSubDir);
                FileKFGame = Path.Combine(DirectoryConfig, KFGame);
                FileKFEngine = Path.Combine(DirectoryConfig, KFEngine);
                FileKFWeb = Path.Combine(DirectoryConfig, KFWeb);
                Runner = new() { StartInfo = new(KFServer, $"{Maps!.Random()}{(GameMode is not null ? "?Game=" + GameMode?.Decode() : string.Empty)}{(AdminPassword is not null ? "?AdminPassword=" + AdminPassword : string.Empty)}{(Offset is not null ? "?Port=" + (Base + Offset) : string.Empty)}{(OffsetWebAdmin is not null ? "?WebAdminPort=" + (AdminBase + OffsetWebAdmin) : string.Empty)}{(ConfigSubDir is not null ? "?ConfigSubDir=" + ConfigSubDir : string.Empty)} -log={Log}") };
            }

            Log = Path.Combine(Logs, Log);
            HackINIs();

            while (true)
            {
                Runner.Start();
                while (!(FileSystem.Exists(Log) && (Lines = FileSystem.TryRead(Log)).Any(_ => _.Contains(PublicIP))))
                    Thread.Sleep(new TimeSpan(0, 1, 0));
                if (HackINIs())
                    Runner.Kill();
                else
                {
                    Address = IPAddress.Parse(Find(Lines, PublicIP));
                    this.Maps ??= INI.GetValue(ContentKFGame, GameInfo, MapCycles).Split('"')[1..^1].Where(_ => ',' != _[0]);
                    Weekly = uint.Parse(Find(Lines, ProbeMode ? IntendedWeekly : UsedWeekly));
                    break;
                }
            }
        }

        bool HackINIsProbe()
        {
            if (!TryReadINIsProbe())
                return true;
            var HackedKFGame = INI.TrySet(ContentKFGame, "Engine.AccessControl", "GamePassword", GamePassword!);
            var HackedKFEngine = INI.TrySet(ContentKFEngine, "Engine.GameEngine", "bUsedForTakeover", false);
            if (HackedKFGame)
                File.WriteAllLines(FileKFGame, ContentKFGame, Encoding.ASCII);
            if (HackedKFEngine)
                File.WriteAllLines(FileKFEngine, ContentKFEngine, Encoding.ASCII);
            return HackedKFGame || HackedKFEngine;
        }

        bool TryReadINIsProbe() => FileSystem.TryRead(FileKFGame, ref ContentKFGame) && FileSystem.TryRead(FileKFEngine, ref ContentKFEngine);

        bool HackINIsProd()
        {
            if (!(TryReadINIsProbe() && FileSystem.TryRead(FileKFWeb!, ref ContentKFWeb)))
                return true;
            var HackedKFGame =
            (AdminPassword is not null && INI.TrySet(ContentKFGame!, EngineInfo, "bAdminCanPause", true)) |
            (ServerName is not null && INI.TrySet(ContentKFGame!, "Engine.GameReplicationInfo", "ServerName", ServerName)) |
            (BannerLink is not null && INI.TrySet(ContentKFGame!, GameInfo, "BannerLink", BannerLink)) |
            (ServerMOTD is not null && INI.TrySet(ContentKFGame!, GameInfo, "ServerMOTD", string.Join("\\n", ServerMOTD))) |
            (WebsiteLink is not null && INI.TrySet(ContentKFGame!, GameInfo, "WebsiteLink", WebsiteLink)) |
            (GameLength is not null && INI.TrySet(ContentKFGame!, GameInfo, "GameLength", (int)GameLength)) |
            (Difficulty is not null && INI.TrySet(ContentKFGame!, EngineInfo, "GameDifficulty", (double)Difficulty)) |
            INI.TrySet(ContentKFGame!, GameInfo, "ClanMotto", string.Empty) |
            INI.TrySet(ContentKFGame!, GameInfo, "bDisableTeamCollision", true) |
            INI.TrySet(ContentKFGame!, GameInfo, MapCycles, INI.Encode(Maps!)) |
            INI.TrySet(ContentKFGame!, "Engine.AccessControl", "GamePassword", GamePassword ?? string.Empty);
            var HackedKFEngine = UsedForTakeover is not null && INI.TrySet(ContentKFEngine!, "Engine.GameEngine", "bUsedForTakeover", UsedForTakeover!.Value);
            var HackedKFWeb = Maps is not null && INI.TrySet(ContentKFWeb!, "IpDrv.WebServer", "bEnabled", AdminPassword is not null);
            if (HackedKFGame)
                File.WriteAllLines(FileKFGame!, ContentKFGame!, Encoding.ASCII);
            if (HackedKFEngine)
                File.WriteAllLines(FileKFEngine!, ContentKFEngine!, Encoding.ASCII);
            if (HackedKFWeb)
                File.WriteAllLines(FileKFWeb!, ContentKFWeb!, Encoding.ASCII);
            return HackedKFGame || HackedKFEngine || HackedKFWeb;
        }

        bool HackINIs() => ProbeMode ? HackINIsProbe() : HackINIsProd();
    }
    #endregion

    static bool RunSteamCMD(int AppID, string? UserName = null)
    {
        if (!File.Exists(SteamCMD))
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    MemoryStream Stream = new();
                    new HttpClient().GetAsync(URL).Result.Content.CopyTo(Stream, null, new CancellationTokenSource().Token);
                    new ZipArchive(Stream).ExtractToDirectory(CWD);
                    break;
                case PlatformID.Unix://TODO: Port to TarFile
                    if (!Directory.Exists(CWD))
                        Directory.CreateDirectory(CWD);
                    var Temp = Path.Combine(CWD, Path.ChangeExtension(SteamCMD, "tar.gz"));
                    try
                    {
                        using (FileStream Writer = new(Temp, FileMode.Create))
                            new HttpClient().GetAsync(URL).Result.Content.CopyTo(Writer, null, new CancellationTokenSource().Token);
                        Process.Start(new ProcessStartInfo("tar", "-xf " + Temp) { WorkingDirectory = CWD })!.WaitForExit();
                    }
                    finally
                    {
                        File.Delete(Temp);
                    }
                    break;
                default:
                    throw new PlatformNotSupportedException();
            }

        bool Result = false;
        //TODO: validate/fix on Linux
        Process Runner = new()
        {
            StartInfo = new ProcessStartInfo(SteamCMD, $"+login {UserName ?? "anonymous"} +app_update {AppID} +quit") { UseShellExecute = OperatingSystem.IsLinux(), RedirectStandardOutput = true }!
        };
        Runner.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
        {
            Console.WriteLine(e.Data);
            Result |= e.Data?.StartsWith(Success) ?? false;
        };
        Runner.Start();
        Runner.BeginOutputReadLine();
        Runner.WaitForExit();
        return Result;
    }

    #region Setup
    #region General
    static readonly string CWD = Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Common.Name),
        PlatformID.Unix => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Steam"),
        _ => throw new PlatformNotSupportedException()
    };
    #endregion

    #region SteamCMD
    const int AppID = 232130;
    const string Success = "Success!";

    static readonly string SteamCMD = Path.Combine(CWD, Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => "steamcmd.exe",
        PlatformID.Unix => "steamcmd.sh",
        _ => throw new PlatformNotSupportedException()
    });

    static readonly string URL = Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip",
        PlatformID.Unix => "https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz",
        _ => throw new PlatformNotSupportedException()
    };

    static readonly string Dumps = Path.Combine(CWD, Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => "dumps",
        _ => throw new PlatformNotSupportedException()
    });
    #endregion

    #region KF2Server
    static readonly string KFServer = Path.Combine(CWD, Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => @"steamapps\common\kf2server\Binaries\Win64\KFServer.exe",
        PlatformID.Unix => "steamapps/common/kf2server/Binaries/Win64/KFGameSteamServer.bin.x86_64",
        _ => throw new PlatformNotSupportedException()
    });

    static readonly string Logs = Path.Combine(CWD, Environment.OSVersion.Platform switch
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

    static readonly string KFGame = Prefix + "Server-KFGame.ini";
    static readonly string KFEngine = Prefix + "Server-KFEngine.ini";
    const string KFWeb = "KFWeb.ini";
    const string GameInfo = "KFGame.KFGameInfo";
    const string EngineInfo = "Engine.GameInfo";
    const string MapCycles = "GameMapCycles";
    const string PublicIP = "Public IP";
    const string IntendedWeekly = "Intended weekly index:";
    const string UsedWeekly = "USED Weekly index:";
    const string Extension = "log";

    static readonly string Config = Path.Combine(CWD, Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => @"steamapps\common\kf2server\KFGame\Config",
        _ => throw new PlatformNotSupportedException()
    });
    #endregion
    #endregion
}

#region Types
public enum GameModes
{
    Survival = 0,
    [EnumMember(Value = "KFGameContent.KFGameInfo_WeeklySurvival")]
    WeeklyOutbreak = 2,
    [EnumMember(Value = "KFGameContent.KFGameInfo_Endless")]
    Endless = 3,
};

public enum Difficulties
{
    Normal = 0,
    Hard = 1,
    Suicidal = 2,
    HellOnEarth = 3,
}
public enum Lengths
{
    Short = 0,
    Normal = 1,
    Long = 2,
}
#endregion

public static class ExtensionMethods
{
    internal static string Decode<T>(this T Enum) where T : Enum => typeof(T).GetMember(Enum!.ToString()!).Single().GetCustomAttributes(false).OfType<EnumMemberAttribute>().Single().Value!;

    internal static IEnumerable<T> Shuffle<T>(this IEnumerable<T> Collection) => Collection.OrderBy(_ => PRNG.Next());

    public static T Random<T>(this IEnumerable<T> Collection) => Collection.ElementAt(PRNG.Next(0, Collection.Count()));

    static readonly Random PRNG = new();
}