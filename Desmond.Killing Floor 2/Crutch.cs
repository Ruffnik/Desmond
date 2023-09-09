using System.Net;
using System.Text;

namespace Desmond;

internal class Crutch
{
#pragma warning disable IDE0060
    internal static void Start(IEnumerable<KF2Server> Farm, IEnumerable<(string, byte[])> Resources)
#pragma warning restore IDE0060
    {
        if (!string.IsNullOrEmpty(Settings.Default.WWWRoot))
        {
            if (!Directory.Exists(Settings.Default.WWWRoot))
                Directory.CreateDirectory(Settings.Default.WWWRoot);
            Resources.ToList().ForEach(_ =>
            {
                var Target = Path.Combine(Settings.Default.WWWRoot, Path.GetFileName(_.Item1));
                if (!File.Exists(Target))
                    File.WriteAllBytesAsync(Target, _.Item2);
            });
        }
    }

    internal static void Update(IEnumerable<KF2Server> Farm, IPAddress Address) => File.WriteAllTextAsync(Path.Combine(Settings.Default.WWWRoot, "index.html"), GetHTML(Farm, Address), Encoding.UTF8);

    static string GetHTML(IEnumerable<KF2Server> Farm, IPAddress Address)
    {
        var Title = $"<title>{string.Join(" | ", Farm.Select(_ => _.ServerName!).Distinct())}</title>";

        var Links = "<link rel=\"shortcut icon\" href=\"favicon.ico\" type=\"image/x-icon\"><link rel=\"stylesheet\" type=\"text/css\" href=\"kf2.css\"><link rel=\"stylesheet\" type=\"text/css\" href=\"kf2modern.css\">";

        var Script = $"<script type=\"text/javascript\">function WebAdmin(Port){{window.location.replace(window.location.protocol +\"//\"+window.location.hostname+\":\"+Port)}}</script>";

        var Table = "<table><tr>" +
            string.Join("</tr><tr>", Farm.Select(Server =>
                $"<td><a href=\"steam://rungameid/232090//-SteamConnectIP={Address}:{Server.Port}\">{Server.ConfigSubDir}</a></td>" +
                //$"<td><a href=\"steam://connect/{Address}:{Server.Port}\">&#x267F;</a></td>" +//TODO: connect in-game somehow
                $"{"<td>"}{(Server.AdminPort is not null ?
                    $"<a href=# onclick=\"WebAdmin(" + Server.AdminPort + ")\">&#x1F9D9</a>" :
                    "&#x274C")}"+
                    "</td>"
        )) + "</tr></table>";

        var Footer = "<footer>" + DateTime.Now.ToString("o") + "</footer>";

        return $"<!doctype html><head>{Title}{Links}{Script}</head><body>{Table}{Footer}</body></html>";
    }
}