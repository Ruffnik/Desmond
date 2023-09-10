using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;

namespace Desmond;

internal static class Frontend
{
    #region Interface
    internal static void Start(IEnumerable<KF2Server> Farm)
    {
        Task.Run(() =>
        {

            Update(Farm);
            TcpListener Listener = new(IPAddress.Any, 80);
            Listener.Start();
            while (true)
                Task.Run(() => Respond(Listener.AcceptTcpClient()));
        });
    }

    internal static void Update(IEnumerable<KF2Server> Farm, IPAddress? Address = null)
    {
        var Title = $"<title>{string.Join(" | ", Farm.Select(_ => _.ServerName!).Distinct())}</title>";

        var Links = "<link rel=\"shortcut icon\" href=\"favicon.ico\" type=\"image/x-icon\"><link rel=\"stylesheet\" type=\"text/css\" href=\"kf2.css\"><link rel=\"stylesheet\" type=\"text/css\" href=\"kf2modern.css\">";

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

        Homepage = $"<!doctype html><head>{Title}{Links}{Script}</head><body>{Table}{Footer}</body></html>";
    }
    #endregion

    #region HTTP
    static Response Serve(string? Path) =>
        Path is null ?
        ServeUnsupported() :
        string.Empty == Path ?
        ServeHomepage() :
        ServeResource(Path);

    static Response ServeHomepage() => new() { Content = Homepage, Type = Types.HTML, Status = Statuses.OK };

    static Response ServeResource(string Name)
    {
        var Type = GetType(Name);
        if (Type is not null)
            if (ResourceExists(Name))
                return new() { Status = Statuses.OK, Type = Type, Content = GetResource(Name) };
            else
                return new() { Status = Statuses.NotFound };
        else
            return ServeUnsupported();
    }

    static Response ServeUnsupported() => new() { Status = Statuses.Unsupported };

    static Types? GetType(string Name)
    {
        foreach (var _ in Enum.GetValues(typeof(Types)))
        {
            if (string.Equals(((Types)_).Decode().Split('/')[1], Path.GetExtension(Name).TrimStart('.'), StringComparison.InvariantCultureIgnoreCase))
                return (Types)_;
        }
        return null;
    }

    static byte[] Encode(Response Response)
    {
        var Result= $@"HTTP/1.0 {(int)Response.Status} {Response.Status.Decode()}";
        if (Statuses.OK == Response.Status)
            Result += @$"
Content-Length: {Response.Content!.Length}
Content-Type: {Response.Type!.Value.Decode()}

{Response.Content}
";
        return Encoding.UTF8.GetBytes(Result);
    }

    static string? GetPath(string Request) => Request.StartsWith("GET") ? Request.Split(' ')[1].Trim('/') : null;
    #endregion

    #region Plumbing
    static readonly Action<TcpClient> Respond = Client =>
    {
        var Buffer = new byte[10240];
        var Stream = Client.GetStream();
        var Length = Stream.Read(Buffer, 0, Buffer.Length);
        var Request = Encoding.UTF8.GetString(Buffer, 0, Length);

        var Path = GetPath(Request);
        var Response = Serve(Path);
        var Scrap = Encode(Response);

        Stream.Write(Scrap);
    };

    static bool ResourceExists(string Name) => Resources.Any(_ => string.Equals(_.Name, Name, StringComparison.InvariantCultureIgnoreCase));

    static string GetResource(string Name) => Resources.Single(_ => string.Equals(_.Name, Name, StringComparison.InvariantCultureIgnoreCase)).Content;

    readonly static IEnumerable<Resource> Resources = Const.Resources.Select(_ => new Resource() { Name = Path.GetFileName(_), Content = File.ReadAllText(_) });

    static string Homepage = string.Empty;
    #endregion

    #region Types
    record Response
    {
        internal string? Content;
        internal Types? Type;
        internal required Statuses Status;
    }

    enum Types
    {
        [EnumMember(Value = "text/html")]
        HTML,
        [EnumMember(Value = "text/css")]
        CSS,
    };

    enum Statuses
    {
        OK = 200,
        [EnumMember(Value = "Not Found")]
        NotFound = 404,
        [EnumMember(Value = "Unsupported Media Type")]
        Unsupported = 415,
    }

    record Resource
    {
        required internal string Name;
        required internal string Content;
    }
    #endregion
}