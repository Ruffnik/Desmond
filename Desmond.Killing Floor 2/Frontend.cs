using System.Net;
using System.Runtime.Serialization;
using System.Text;

namespace Desmond;

internal static class Frontend
{
    #region Business logic
    internal static void Start(IEnumerable<KF2Server> Farm)
    {
        Task.Run(() =>
        {
            Update(Farm);
            HttpListener Listener = new();
            Listener.Prefixes.Add("http://*:80/");
            Listener.Start();
            while (true)
                Task.Run(() => Respond(Listener.GetContext()));
        });
    }

    internal static void Update(IEnumerable<KF2Server> Farm, IPAddress? Address = null)
    {
        if (Resources is null && !Const.Resources.Where(_ => !Path.Exists(_)).Any())
            Resources = Const.Resources.Select(_ => new Resource() { Name = Path.GetFileName(_), Content = File.ReadAllBytes(_) });

        var Title = $"<title>{string.Join(" | ", Farm.Select(_ => _.ServerName!).Distinct())}</title>";

        var Links = $"<link rel=\"icon\" href=\"favicon.ico\" type=\"{Types.ICO.Decode()}\"><link rel=\"stylesheet\" type=\"{Types.CSS.Decode()}\" href=\"kf2.css\"><link rel=\"stylesheet\" type=\"{Types.CSS.Decode()}\" href=\"kf2modern.css\">";

        var Script = $"<script type=\"text/javascript\">function WebAdmin(Port){{window.location.replace(window.location.protocol +\"//\"+window.location.hostname+\":\"+Port)}}</script>";

        var Table = "<table><tr>" +
            string.Join("</tr><tr>", Farm.Select(Server =>
            {
                var Opener = null == Address ? string.Empty : $"<a href=\"steam://rungameid/232090//-SteamConnectIP={Address}:{Server.Port}\">";
                var Closer = null == Address ? string.Empty : "</a>";

                return $"<td>{Opener}{Server.ConfigSubDir}{Closer}</td>" +
               //$"<td><a href=\"steam://connect/{Address}:{Server.Port}\">&#x267F;</a></td>" +//TODO: connect in-game somehow
               $"{"<td>"}{(Server.AdminPort is not null ?
                   $"<a href=# onclick=\"WebAdmin(" + Server.AdminPort + ")\">&#x1F9D9</a>" :
                   "&#x274C")}" +
                   "</td>";
            }
        )) + "</tr></table>";

        var Footer = "<footer>" + DateTime.Now.ToString("o") + "</footer>";

        Homepage = Encoding.UTF8.GetBytes($"<!doctype html><head>{Title}{Links}{Script}</head><body>{Table}{Footer}</body></html>");
    }
    #endregion

    #region HTTP
    static Response Serve(string? Path) =>
        Path is null ?
        new() { Status = HttpStatusCode.NotFound } :
        string.Empty == Path ?
        ServeHomepage() :
        ServeResource(Path);

    static Response ServeHomepage() => new() { Content = Homepage, Type = Types.HTML, Status = HttpStatusCode.OK };

    static Response ServeResource(string Name)
    {
        var Type = GetType(Name);
        if (Type is not null && ResourceExists(Name))
            return new() { Status = HttpStatusCode.OK, Type = Type, Content = GetResource(Name) };
        else
            return new() { Status = HttpStatusCode.NotFound };
    }

    static Types? GetType(string Name)
    {
        foreach (var _ in Enum.GetValues(typeof(Types)))
        {
            if (string.Equals($"{(Types)_}", Path.GetExtension(Name).TrimStart('.'), StringComparison.InvariantCultureIgnoreCase))
                return (Types)_;
        }
        return null;
    }
    #endregion

    #region Plumbing
    static readonly Action<HttpListenerContext> Respond = Context =>
    {
        var Payload = Serve(Context.Request.RawUrl?.Trim('/'));
        var Response = Context.Response;
        Response.StatusCode = (int)Payload.Status;
        if (HttpStatusCode.OK == Payload.Status)
        {
            Response.ContentLength64 = Payload.Content!.LongLength;
            Response.ContentType = ((Types)Payload.Type!).Decode();
        }
        var Buffer = Payload.Content;
        using var Stream = Response.OutputStream;
        Stream.Write(Buffer, 0, Buffer.Length);
    };

    static bool ResourceExists(string Name) => Resources?.Any(_ => string.Equals(_.Name, Name, StringComparison.InvariantCultureIgnoreCase)) ?? false;

    static byte[] GetResource(string Name) => Resources!.Single(_ => string.Equals(_.Name, Name, StringComparison.InvariantCultureIgnoreCase)).Content;

    static IEnumerable<Resource>? Resources;

    static byte[] Homepage = Array.Empty<byte>();
    #endregion

    #region Types
    record Response
    {
        internal byte[] Content = Array.Empty<byte>();
        internal Types? Type;
        internal required HttpStatusCode Status;
    }

    enum Types
    {
        [EnumMember(Value = "text/html")]
        HTML,
        [EnumMember(Value = "text/css")]
        CSS,
        [EnumMember(Value = "image/x-icon")]
        ICO,
        [EnumMember(Value = "image/png")]
        PNG
    };

    record Resource
    {
        required internal string Name;
        required internal byte[] Content;
    }
    #endregion
}