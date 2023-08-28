using System.Text;

namespace Desmond;

internal class FileSystem
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

    internal static bool Exists(string Path)//TODO: fix what I think to be a race condition in a more structured way (current solution stems from trial & error)
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