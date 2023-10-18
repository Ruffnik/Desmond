﻿using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;

namespace Desmond;

internal static class Frontend
{
    #region Business logic
    internal static void Start(IEnumerable<KF2Server> Farm) => Task.Run(async () =>
                                                                    {
                                                                        Update(Farm);
                                                                        TcpListener Server = new(IPAddress.Any, Const.Port);
                                                                        Server.Start();

                                                                        while (true)
                                                                        {
                                                                            var Client = await Server.AcceptTcpClientAsync();
                                                                            await Task.Run(() =>
                                                                            {
                                                                                using var Stream = Client.GetStream();
                                                                                var Response = ServePath(DecodePath(Encoding.UTF8.GetString(Stream.ReadAll())));
                                                                                Stream.Write(Encode(Response));
                                                                            });
                                                                        }
                                                                    });

    internal static void Update(IEnumerable<KF2Server> Farm, IPAddress? Address = null)
    {
        _Farm = Farm;

        var Title = $"<title>{string.Join(" | ", Farm.Select(_ => _.ServerName!).Distinct())}</title>";

        var Links = $"<link rel=\"icon\" type=\"{Types.ICO.Decode()}\" href=\"favicon.ico\"><link rel=\"stylesheet\" type=\"{Types.CSS.Decode()}\" href=\"kf2.css\"><link rel=\"stylesheet\" type=\"{Types.CSS.Decode()}\" href=\"kf2modern.css\">";

        //var Script = $"<script type=\"text/javascript\">function WebAdmin(Port){{window.location.replace(window.location.protocol +\"//\"+window.location.hostname+\":\"+Port)}}</script>";

        var Table = "<table><tr>" +
            string.Join("</tr><tr>", Farm.Select(Server =>
            {
                var Connect = Address is null ? string.Empty : $"<a href=\"steam://rungameid/232090//-SteamConnectIP={Address}:{Server.Port}\">";
                var Closer = Address is null ? string.Empty : "</a>";
                var Administrate = Address is null ? string.Empty : $"<a href=\"http://{Address}:{Server.AdminPort}\">";

                return
               //$"<td><a href=\"steam://connect/{Address}:{Server.Port}\">&#x267F;</a></td>" +//TODO: connect in-game somehow
               $"{"<td>"}{(Server.AdminPort is not null ?
                   $"{Administrate}&#x1F9D9{Closer}" :
                   "&#x274C")}</td>" +
                   $"<td>{Connect}{Server.ConfigSubDir}{Closer}</td>";
            }
        )) + "</tr></table>";

        var Footer = "<footer>" + DateTime.Now.ToString("o") + "</footer>";

        Homepage = $"<!doctype html><head>{Title}{Links}</head><body>{Table}{Footer}</body></html>";
    }
    #endregion

    #region HTTP
    static string? DecodePath(string Request)
    {
        var Parts = Request.Split(' ');
        return "GET" == Parts[0] ? Parts[1].TrimStart('/').Replace('/', Path.DirectorySeparatorChar) : null;
    }

    static byte[] Encode(Response Response)
    {
        IEnumerable<byte> Result = Encoding.UTF8.GetBytes(@$"HTTP/1.1 {(int)Response.Status} {Response.Status}
");
        if (HttpStatusCode.OK == Response.Status)
            Result = Result.Concat(Encoding.UTF8.GetBytes(@$"Content-Length: {Response.Content!.LongLength}
Content-Type: {Response.Type!.Value.Decode()}

")).Concat(Response.Content!);
        return Result.ToArray();
    }

    static Response ServePath(string? Path) =>
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

    static IEnumerable<KF2Server>? _Farm;
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