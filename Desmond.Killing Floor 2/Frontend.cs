using System.Net;
using System.Runtime.Serialization;
using System.Text;

namespace Desmond;

internal static class Frontend
{
    #region Business logic
    internal static void Start(IEnumerable<KF2Server> Farm)
    {
        //Update(Farm);
        //HttpListener Listener = new() { IgnoreWriteExceptions = true };
        //Listener.Prefixes.Add("http://*:8080/");
        //Listener.Start();
        //Listener.BeginGetContext(new(Callback), Listener);
    }

    internal static void Update(IEnumerable<KF2Server> Farm, IPAddress? Address = null)
    {
        //var Title = $"<title>{string.Join(" | ", Farm.Select(_ => _.ServerName!).Distinct())}</title>";

        //var Links = $"<link rel=\"icon\" type=\"{Types.ICO.Decode()}\" href=\"favicon.ico\"><link rel=\"stylesheet\" type=\"{Types.CSS.Decode()}\" href=\"kf2.css\"><link rel=\"stylesheet\" type=\"{Types.CSS.Decode()}\" href=\"kf2modern.css\">";

        //var Script = $"<script type=\"text/javascript\">function WebAdmin(Port){{window.location.replace(window.location.protocol +\"//\"+window.location.hostname+\":\"+Port)}}</script>";

        //var Table = "<table><tr>" +
        //    string.Join("</tr><tr>", Farm.Select(Server =>
        //    {
        //        var Opener = null == Address ? string.Empty : $"<a href=\"steam://rungameid/232090//-SteamConnectIP={Address}:{Server.Port}\">";
        //        var Closer = null == Address ? string.Empty : "</a>";

        //        return $"<td>{Opener}{Server.ConfigSubDir}{Closer}</td>" +
        //       //$"<td><a href=\"steam://connect/{Address}:{Server.Port}\">&#x267F;</a></td>" +//TODO: connect in-game somehow
        //       $"{"<td>"}{(Server.AdminPort is not null ?
        //           $"<a href=# onclick=\"WebAdmin(" + Server.AdminPort + ")\">&#x1F9D9</a>" :
        //           "&#x274C")}" +
        //           "</td>";
        //    }
        //)) + "</tr></table>";

        //var Footer = "<footer>" + DateTime.Now.ToString("o") + "</footer>";

        //Homepage = $"<!doctype html><head>{Title}{Links}{Script}</head><body>{Table}{Footer}</body></html>";
    }

    static void Callback(IAsyncResult _)
    {
        var Listener = (HttpListener)_.AsyncState!;
        Listener.BeginGetContext(new AsyncCallback(Callback), Listener);
        var Context = Listener.EndGetContext(_);

        var Payload = Serve(Context.Request.RawUrl?.TrimStart('/'));
        var Response = Context.Response;
        Response.StatusCode = (int)Payload.Status;
        using var Stream = Response.OutputStream;
        if (HttpStatusCode.OK == Payload.Status)
        {
            Response.ContentLength64 = Payload.Content!.LongLength;
            Response.ContentType = Payload.Type!.Value.Decode();
            Stream.Write(Payload.Content, 0, Payload.Content.Length);
        }
    }
    #endregion

    #region HTTP
    static Response Serve(string? Path) =>
        Path is null ?
        new(HttpStatusCode.BadRequest) :
        string.Empty == Path ?
        new(Homepage!) :
        ServeResource(Path);

    static Response ServeResource(string Name)
    {
        var Type = GetType(Name);
        if (Type is not null)
        {
            if (Resources.TryGetValue(Name, out var Content))
                return new(Type.Value, Content);
            else
            {
                var ODS = Path.Combine(Const.WWWRoot, Name);
                if (File.Exists(ODS))
                {
                    Content = File.ReadAllBytes(ODS);
                    Resources.Add(Name, Content);
                    return new(Type.Value, Content);
                }
                else
                    return new(HttpStatusCode.NotFound);
            }
        }
        else
            return new(HttpStatusCode.UnsupportedMediaType);
    }

    static Types? GetType(string Name)
    {
        foreach (var _ in Enum.GetValues(typeof(Types)))
            if (string.Equals($"{(Types)_}", Path.GetExtension(Name).TrimStart('.'), StringComparison.InvariantCultureIgnoreCase))
                return (Types)_;
        return null;
    }
    #endregion

    #region Plumbing
    static readonly Dictionary<string, byte[]> Resources = new();

    static string? Homepage;
    #endregion

    #region Types
    record Response
    {
        internal readonly byte[]? Content;
        internal readonly Types? Type;
        internal readonly HttpStatusCode Status;

        internal Response(string HTML) : this(Types.HTML, Encoding.UTF8.GetBytes(HTML)) { }

        internal Response(Types Type, byte[] Content)
        {
            Status = HttpStatusCode.OK;
            this.Type = Type;
            this.Content = Content;
        }

        internal Response(HttpStatusCode Status)
        {
            if (HttpStatusCode.OK != Status)
                this.Status = Status;
            else
                throw new ArgumentException($"{Status}", nameof(Status));
        }
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
    #endregion
}