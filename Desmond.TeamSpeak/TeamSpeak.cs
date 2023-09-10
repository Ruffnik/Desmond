using System.Diagnostics;
using System.IO.Compression;
using Desmond;
using Microsoft.VisualBasic.FileIO;

AppDomain.CurrentDomain.UnhandledException += (object _, UnhandledExceptionEventArgs e) => Console.WriteLine($"{e}");
Console.Title = Utilities.Name;

#region Setup
string CWD = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Utilities.Name);
const string URL = "https://files.teamspeak-services.com/releases/server/";
string SubDir = Path.Combine(CWD, "teamspeak3-server_win64");
string Logs = Path.Combine(SubDir, "logs");
string Changelog = Path.Combine(SubDir, "changelog.txt");
string Binary = Path.Combine(SubDir, "ts3server.exe");
const string Header = "## Server Release";
#endregion

Kill();
while (true)
{
    Clean();
    TryUpdate();
    Run();
    Thread.Sleep(new TimeSpan(1, 0, 0));
}

#region Business logic
void TryUpdate()
{
    var Latest = GetLatest();
    if (double.Parse(Latest) > GetCurrent())
    {
        Kill();
        MemoryStream Stream = new();
        new HttpClient().GetAsync(URL + Latest + "/teamspeak3-server_win64-" + Latest + ".zip").Result.Content.CopyTo(Stream, null, new CancellationTokenSource().Token);
        var Temp = Path.GetTempFileName();
        File.Delete(Temp);
        new ZipArchive(Stream).ExtractToDirectory(Temp);
        FileSystem.MoveDirectory(Temp, CWD, true);
    }
}

void Run()
{
    if (!Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Binary)).Any())
        Process.Start(new ProcessStartInfo(Binary) { WorkingDirectory = SubDir });
}

void Clean()
{
    if (Directory.Exists(Logs))
        Task.Run(() => new DirectoryInfo(Logs).GetFiles().ToList().ForEach(_ => Utilities.TryDelete(_.FullName)));
}
#endregion

#region Plumbing
static string GetLatest() => new string(new HttpClient().GetAsync(URL).Result.Content.ReadAsStringAsync().Result.ToCharArray().Where(Char => !char.IsWhiteSpace(Char)).ToArray()).Split("<ahref=\"").Select(Part => Part.Split('"')[0]).Where(Release => double.TryParse(Release, out var Scrap)).MaxBy(double.Parse) ?? throw new NotImplementedException();

double GetCurrent() => File.Exists(Changelog) ? File.ReadAllLines(Changelog).Where(Line => Line.StartsWith(Header)).Select(Line => Line.Replace(Header, string.Empty).Split(' ')[1]).Select(Line => double.Parse(Line.Replace(Header, string.Empty).Split(' ')[0])).Max() : 0;

void Kill() => Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Binary)).ToList().ForEach(_ => _.Kill());
#endregion