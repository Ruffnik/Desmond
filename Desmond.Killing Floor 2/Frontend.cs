using System.Net;

namespace Desmond;

internal class Frontend
{
    internal static void Start(IEnumerable<KF2Server> Farm)
    {
        Task.Run(() =>
        {
            var Resources = Const.Resources.Select(_ => (Path.GetFileName(_), File.ReadAllBytes(_)));

            Crutch.Start(Farm, Resources);

            //TODO: look up all that HTTP stuff to send out CSS/ICO as well as HTML
            //TcpListener Listener = new(IPAddress.Any, 80);
            //Listener.Start();

            //while (true)
            //{
            //    var Client = Listener.AcceptTcpClient();
            //    var Buffer = new byte[10240];
            //    var Stream = Client.GetStream();
            //    var Length = Stream.Read(Buffer, 0, Buffer.Length);
            //    var Request = Encoding.UTF8.GetString(Buffer, 0, Length);

            //    var Path = GetPath(Request);
            //    if (Resources.Any(_ => _.Item1 == Path))
            //        Stream.Write(Resources.Single(_ => _.Item1 == Path).Item2);
            //    else
            //        Stream.Write(
            //            Encoding.UTF8.GetBytes(
            //                "HTTP/1.0 200 OK" + Environment.NewLine
            //                + "Content-Length: " + Page.Length + Environment.NewLine
            //                + "Content-Type: " + "text/html" + Environment.NewLine
            //                + Environment.NewLine
            //                + GetHTML()
            //                + Environment.NewLine + Environment.NewLine));
            //}
        });
    }

    internal static void Update(IEnumerable<KF2Server> Farm, IPAddress Address) => Crutch.Update(Farm, Address);

    //static string? GetPath(string Request)
    //{
    //    if (Request.StartsWith("GET"))
    //    {
    //        var Result = Request.Split(' ')[1].Trim('/');
    //        if (string.IsNullOrEmpty(Result))
    //            return null;
    //        else return Result;
    //    }
    //    else
    //        return null;
    //}
}