using System.ComponentModel;
using Desmond;

AppDomain.CurrentDomain.UnhandledException += (object _, UnhandledExceptionEventArgs e) => Console.WriteLine($"{e}");
Settings.Default.PropertyChanged += (object? sender, PropertyChangedEventArgs e) => Settings.Default.Save();

Console.Title = Utilities.Name;
string CWD = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Utilities.Name);

var Farm = Enumerable.Empty<KF2Server>();
if (!Directory.Exists(CWD))
    Directory.CreateDirectory(CWD);
else
{
    Directory.EnumerateFiles(CWD, "*." + Const.XML).ToList().ForEach(_ =>
    {
        var Server = Utilities.Deserialize<KF2Server>(_);
        Server.ConfigSubDir = Path.GetFileNameWithoutExtension(_);
        Server.Offset = Farm.Count();
        if (Server.AdminPassword is not null)
            Server.AdminOffset = Farm.Where(Server => Server.AdminPassword is not null).Count();
        Farm = Farm.Append(Server);
    });
}

Frontend.Start(Farm);
KF2Server.KillAll();

while (true)
{
    KF2Server.Clean();
    KF2Server.TryUpdate();
    var Status = KF2Server.GetStatus();
    Frontend.Update(Farm, Status.Address);

    var MissingStockMaps = Status.Maps.Where(_ => !Settings.Default.StockMaps.Contains(_));
    if (MissingStockMaps.Any())
    {
        Settings.Default.StockMaps = Utilities.EncodeSettings(Utilities.DecodeStrings(Settings.Default.StockMaps).Concat(MissingStockMaps.Shuffle()));
        Utilities.Serialize(Path.ChangeExtension(nameof(Settings.Default.StockMaps), Const.XML), Path.Combine(CWD, Path.ChangeExtension(string.Join(string.Empty, DateOnly.FromDateTime(DateTime.Now).ToString("o").Split(Path.GetInvalidFileNameChars())), "zip")), Settings.Default.StockMaps);
    }

    Farm.AsParallel().ForAll(_ =>
    {
        if (_.Weekly is not null && _.Weekly != Status.Weekly)
            _.Kill();
        _.Run(Utilities.DecodeStrings(Settings.Default.StockMaps));
    });
    Task.WaitAny(new[]
        {
                Task.Delay(new TimeSpan(1, 0, 0)),
                Task.Run(()=>Farm.ToList().ForEach(_ => _.Wait()))
        });
}