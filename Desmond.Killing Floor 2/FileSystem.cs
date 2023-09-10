using System.Text;

namespace Desmond;

internal static class FileSystem
{
    internal static bool TryRead(string Path, ref string[] Collection)
    {
        if (File.Exists(Path))
        {
            Collection = File.ReadAllLines(Path, Encoding.ASCII);
            return true;
        }
        else
            return false;
    }

    //Workaround for race condition
    internal static bool Exists(string Path)
    {
        if (!File.Exists(Path))
            return false;
        else
            try
            {
                return 0 < new FileInfo(Path).Length;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
    }

    internal static string[] TryRead(string Path)
    {
        try
        {
            using FileStream _ = new(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return new StreamReader(_).ReadToEnd().Split(Environment.NewLine);
        }
        catch (UnauthorizedAccessException)
        {
            return Array.Empty<string>();
        }
    }
}