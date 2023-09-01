using System.Net;
using System.Text;

namespace Desmond;

internal class Crutch
{
    internal static void Start(IEnumerable<KF2Server> Farm, IEnumerable<(string, byte[])> Resources)
    {
        Settings.Default.Crutch = Settings.Default.Crutch;

        if (!string.IsNullOrEmpty(Settings.Default.Crutch))
        {
            if (!Directory.Exists(Settings.Default.Crutch))
                Directory.CreateDirectory(Settings.Default.Crutch);
            Resources.ToList().ForEach(_ =>
            {
                var Target = Path.Combine(Settings.Default.Crutch, Path.GetFileName(_.Item1));
                if (!File.Exists(Target))
                    File.WriteAllBytesAsync(Target, _.Item2);
            });
        }
    }

    internal static void Update(IEnumerable<KF2Server> Farm, IPAddress Address) => File.WriteAllTextAsync(Path.Combine(Settings.Default.Crutch, "index.html"), GetHTML(Farm, Address), Encoding.UTF8);

    static string GetHTML(IEnumerable<KF2Server> Farm, IPAddress Address)
    {
        static string Title(IEnumerable<KF2Server> Farm) => $"<title>{string.Join(" | ", Farm.Select(_ => _.ServerName!).Distinct())}</title>";

        var Links = "<link rel=\"shortcut icon\" href=\"favicon.ico\" type=\"image/x-icon\"><link rel=\"stylesheet\" type=\"text/css\" href=\"kf2.css\"><link rel=\"stylesheet\" type=\"text/css\" href=\"kf2modern.css\">";

        static string Script(IEnumerable<KF2Server> Farm) => $"<script type=\"text/javascript\">function WebAdmin(Port){{window.location.replace(window.location.protocol +\"//\"+window.location.hostname+\":\"+Port)}}</script>";

        static string Table(IEnumerable<KF2Server> Farm, IPAddress Address) => "<table><tr>" +
            string.Join("</tr><tr>", Farm.Select(Server =>
                $"<td><a href=\"steam://rungameid/232090//-SteamConnectIP={Address}:{Server.Port}\">{Server.ConfigSubDir}</a></td>" +
                //$"<td><a href=\"steam://connect/{Address}:{Server.Port}\">&#x267F;</a></td>" +//TODO: connect in-game somehow
                $"{"<td>"}{(Server.AdminPort is not null ?
                    $"<a href=# onclick=\"WebAdmin(" + Server.AdminPort + ")\">&#x1F9D9</a>" :
                    "&#x274C")}{"</td>"}"
        )) + "</tr></table>";

        var Footer = "<footer>" + DateTime.Now.ToString("o") + "</footer>";

        return $"<!doctype html><head>{Title(Farm)}{Links}{Script(Farm)}</head><body>{Table(Farm, Address)}{Footer}</body></html>";
    }
}