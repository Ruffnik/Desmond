using System.Collections.Specialized;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;

namespace Desmond;

public static class Utilities
{
    public static string Name => Assembly.GetEntryAssembly()!.GetName().Name!;

    public static void Serialize(string FileName, string ArchiveFileName, object Data)
    {
        using var Stream = new FileStream(ArchiveFileName, FileMode.Create);
        using var Archive = new ZipArchive(Stream, ZipArchiveMode.Create);
        var Entry = Archive.CreateEntry(FileName);
        using var Writer = XmlWriter.Create(Entry.Open());
        new DataContractSerializer(Data.GetType()).WriteObject(Writer, Data);
    }

    public static void Serialize(string FileName, object Data)
    {
        using var Stream = new FileStream(FileName, FileMode.Create);
        using var Writer = XmlWriter.Create(Stream);
        new DataContractSerializer(Data.GetType()).WriteObject(Writer, Data);
    }

    public static T Deserialize<T>(string FileName)
    {
        var Stream = new FileStream(FileName, FileMode.Open);
        var Reader = XmlReader.Create(Stream);
        return (T)new DataContractSerializer(typeof(T)).ReadObject(Reader)!;
    }

    public static StringCollection EncodeSettings(IEnumerable<ulong> _) => EncodeSettings(_.Select(_ => $"{_}"));

    public static StringCollection EncodeSettings(IEnumerable<string> _)
    {
        StringCollection Result = new();
        Result.AddRange(_.ToArray());
        return Result;
    }

    public static IEnumerable<ulong> DecodeNumbers(StringCollection _) => _.Cast<string>().Select(ulong.Parse);

    public static IEnumerable<string> DecodeStrings(StringCollection _) => _.Cast<string>().AsEnumerable();

    public static void TryDelete(string Path)
    {
        if (File.Exists(Path))
            try
            {
                File.Delete(Path);
            }
            catch (IOException)
            { }//Don't care if can't delete a particular file at this iteration
    }
}

public static class ExtensionMethods
{
    public static string Decode<T>(this T Enum) where T : Enum => typeof(T).GetMember(Enum!.ToString()!).Single().GetCustomAttributes(false).OfType<EnumMemberAttribute>()!.SingleOrDefault()?.Value ?? $"{Enum}";

    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> Collection) => Collection.OrderBy(_ => PRNG.Next());

    public static T Random<T>(this IEnumerable<T> Collection) => Collection.ElementAt(PRNG.Next(0, Collection.Count()));

    public static byte[] ReadAll(this Stream Stream)
    {
        int Last;
        var Buffer = new byte[10240];
        Last = Stream.Read(Buffer, 0, Buffer.Length);
        Console.WriteLine($"Read {Last} bytes");
        return Buffer[0..Last];
    }

    static readonly Random PRNG = new();
}