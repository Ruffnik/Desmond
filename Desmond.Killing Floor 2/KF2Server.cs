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
    internal static void TryUpdate()
    {
        while (!RunSteamCMD(Const.AppID))
            KillAll();
    }

    internal static void Clean()
    {
        Clean(Const.Logs);
        if (PlatformID.Win32NT == Environment.OSVersion.Platform)
            Clean(Const.Dumps);

        void Clean(string Folder)
        {
            if (Directory.Exists(Folder))
                Task.Run(() => new DirectoryInfo(Folder).GetFiles().ToList().ForEach(_ => Utilities.TryDelete(_.FullName)));
        }
    }

    internal static void KillAll() => Process.GetProcessesByName(Const.Process).ToList().ForEach(_ => _.Kill());

    internal static PersistentState GetStaticState()
    {
        var Runner = new KF2Server(true);
        Runner.Run();
        Runner.Runner!.Kill();
        return new() { Weekly = Runner.Weekly!.Value, Address = Runner.Address!, Maps = Runner.Maps! };
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
    internal int? Offset, AdminOffset;
    internal int? Port { get => Base + Offset; }
    internal int? AdminPort { get => AdminBase + AdminOffset; }
    readonly bool ProbeMode;
    internal IPAddress? Address { get; private set; }
    internal IEnumerable<string>? Maps { get; private set; }
    internal uint? Weekly { get; private set; }

    internal string ConfigSubDir
    {
        get => _ConfigSubDir!;
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

    internal void Kill() => Runner!.Kill();

    internal DynamicState DynamicState
    {
        get
        {
            var Title = Runner!.MainWindowTitle.Replace(Const.Players, string.Empty).Replace(Const.Connections, string.Empty).Split(new[] { ':', '(', ')', ',' }).Select(_ => _.Trim());
            return new() { Map = Title.ElementAt(1), Players = int.Parse(Title.ElementAt(2)), Connections = int.Parse(Title.ElementAt(3)) };
        }
    }
    #endregion

    #region Plumbing
    Process? Runner;
    string? _ConfigSubDir;

    internal KF2Server(bool ProbeMode = false) => this.ProbeMode = ProbeMode;

    static string Find(string[] Lines, string Key) => Lines.Where(_ => _.Contains(Key)).First().Split(Key)[1].Trim().Split(' ')[0];
    #endregion

    internal void Run(IEnumerable<string>? Maps = null)
    {
        string[] ContentKFGame = Array.Empty<string>();
        string[] ContentKFEngine = Array.Empty<string>();
        string[] ContentKFWeb = Array.Empty<string>();
        string[] Lines;
        string FileKFGame, FileKFEngine, FileKFWeb = string.Empty;

        if (!Running)
        {
            FileKFGame = Path.Combine(Const.Config, Const.KFGame);
            FileKFEngine = Path.Combine(Const.Config, Const.KFEngine);
            var Log = Path.ChangeExtension(Path.GetRandomFileName(), Const.Extension);

            if (ProbeMode)
            {
                GamePassword = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
                if (TryReadINIsProbe())
                {
                    INI.TryRemove(ref ContentKFGame, Const.GameInfo, Const.MapCycles);
                    File.WriteAllLines(FileKFGame, ContentKFGame, Encoding.ASCII);
                }
                Runner = new() { StartInfo = new(Const.KFServer, $"-log={Log}") };
            }
            else
            {
                this.Maps = Maps?.ToArray();
                var DirectoryConfig = Path.Combine(Const.Config, ConfigSubDir);
                FileKFGame = Path.Combine(DirectoryConfig, Const.KFGame);
                FileKFEngine = Path.Combine(DirectoryConfig, Const.KFEngine);
                FileKFWeb = Path.Combine(DirectoryConfig, Const.KFWeb);
                Runner = new() { StartInfo = new(Const.KFServer, $"{Maps!.Random()}{(GameMode is not null ? "?Game=" + GameMode?.Decode() : string.Empty)}{(AdminPassword is not null ? "?AdminPassword=" + AdminPassword : string.Empty)}{(Offset is not null ? "?Port=" + Port : string.Empty)}{(AdminOffset is not null ? "?WebAdminPort=" + AdminPort : string.Empty)}{(ConfigSubDir is not null ? "?ConfigSubDir=" + ConfigSubDir : string.Empty)} -log={Log}") };
            }

            Log = Path.Combine(Const.Logs, Log);
            HackINIs();

            while (true)
            {
                Runner.Start();
                while (!(FileSystem.Exists(Log) && (Lines = FileSystem.TryRead(Log)).Any(_ => _.Contains(Const.PublicIP))))
                    Thread.Sleep(new TimeSpan(0, 1, 0));
                if (HackINIs())
                    Runner.Kill();
                else
                {
                    Address = IPAddress.Parse(Find(Lines, Const.PublicIP));
                    this.Maps ??= INI.GetValue(ContentKFGame, Const.GameInfo, Const.MapCycles).Split('"')[1..^1].Where(_ => ',' != _[0]);
                    Weekly = uint.Parse(Find(Lines, ProbeMode ? Const.IntendedWeekly : Const.UsedWeekly));
                    break;
                }
            }
        }

        #region INIs
        bool HackINIs() => ProbeMode ? HackINIsProbe() : HackINIsProd();

        bool HackINIsProbe()
        {
            if (!TryReadINIsProbe())
                return true;
            var HackedKFGame = TrySetGamePassword(GamePassword);
            var HackedKFEngine = TrySetTakeover(false);
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
            (AdminPassword is not null && INI.TrySet(ContentKFGame!, Const.EngineInfo, "bAdminCanPause", true)) |
            (ServerName is not null && INI.TrySet(ContentKFGame!, "Engine.GameReplicationInfo", "ServerName", ServerName)) |
            (BannerLink is not null && INI.TrySet(ContentKFGame!, Const.GameInfo, "BannerLink", BannerLink)) |
            (ServerMOTD is not null && INI.TrySet(ContentKFGame!, Const.GameInfo, "ServerMOTD", string.Join("\\n", ServerMOTD))) |
            (WebsiteLink is not null && INI.TrySet(ContentKFGame!, Const.GameInfo, "WebsiteLink", WebsiteLink)) |
            (GameLength is not null && INI.TrySet(ContentKFGame!, Const.GameInfo, "GameLength", (int)GameLength)) |
            (Difficulty is not null && INI.TrySet(ContentKFGame!, Const.EngineInfo, "GameDifficulty", (double)Difficulty)) |
            INI.TrySet(ContentKFGame!, Const.GameInfo, "ClanMotto", string.Empty) |
            INI.TrySet(ContentKFGame!, Const.GameInfo, "bDisableTeamCollision", true) |
            INI.TrySet(ContentKFGame!, Const.GameInfo, Const.MapCycles, INI.Encode(Maps!)) |
            TrySetGamePassword(GamePassword);
            var HackedKFEngine = TrySetTakeover(UsedForTakeover);
            var HackedKFWeb = Maps is not null && INI.TrySet(ContentKFWeb!, "IpDrv.WebServer", "bEnabled", AdminPassword is not null);
            if (HackedKFGame)
                File.WriteAllLines(FileKFGame!, ContentKFGame!, Encoding.ASCII);
            if (HackedKFEngine)
                File.WriteAllLines(FileKFEngine!, ContentKFEngine!, Encoding.ASCII);
            if (HackedKFWeb)
                File.WriteAllLines(FileKFWeb!, ContentKFWeb!, Encoding.ASCII);
            return HackedKFGame || HackedKFEngine || HackedKFWeb;
        }

        bool TrySetGamePassword(string? GamePassword) => INI.TrySet(ContentKFGame!, "Engine.AccessControl", Const.GamePassword, GamePassword ?? string.Empty);

        bool TrySetTakeover(bool? UsedForTakeover) => UsedForTakeover is not null && INI.TrySet(ContentKFEngine!, "Engine.GameEngine", Const.UsedForTakeover, UsedForTakeover!.Value);
        #endregion
    }
    #endregion

    static bool RunSteamCMD(int AppID, string? UserName = null)
    {
        if (!File.Exists(Const.SteamCMD))
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    MemoryStream Stream = new();
                    new HttpClient().GetAsync(Const.URL).Result.Content.CopyTo(Stream, null, new CancellationTokenSource().Token);
                    new ZipArchive(Stream).ExtractToDirectory(Const.CWD);
                    break;
                case PlatformID.Unix:
                    if (!Directory.Exists(Const.CWD))
                        Directory.CreateDirectory(Const.CWD);
                    var Temp = Path.Combine(Const.CWD, Path.ChangeExtension(Const.SteamCMD, "tar.gz"));
                    try
                    {
                        using (FileStream Writer = new(Temp, FileMode.Create))
                            new HttpClient().GetAsync(Const.URL).Result.Content.CopyTo(Writer, null, new CancellationTokenSource().Token);
                        Process.Start(new ProcessStartInfo("tar", "-xf " + Temp) { WorkingDirectory = Const.CWD })!.WaitForExit();
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
        Process Runner = new()
        {
            StartInfo = new ProcessStartInfo(Const.SteamCMD, $"+login {UserName ?? "anonymous"} +app_update {AppID} +quit")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true
            }!
        };
        Runner.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
        {
            Console.WriteLine(e.Data);
            Result |= e.Data?.StartsWith(Const.Success) ?? false;
        };
        Runner.Start();
        Runner.BeginOutputReadLine();
        Runner.WaitForExit();
        return Result;
    }
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

internal record PersistentState
{
    internal required uint Weekly;
    internal required IPAddress Address;
    internal required IEnumerable<string> Maps;
}

internal record DynamicState
{
    internal required string Map;
    internal required int Players, Connections;
}
#endregion