using System.Globalization;

namespace Desmond;

internal class INI
{
    internal static bool TrySet(string[] Data, string Section, string Key, string Value)
    {
        if (Value != GetValue(Data, Section, Key))
        {
            SetValue(Data, Section, Key, Value);
            return true;
        }
        else
            return false;
    }

    internal static bool TrySet(string[] Data, string Section, string Key, bool Value)
    {
        if (!bool.TryParse(GetValue(Data, Section, Key), out var Result) || Result != Value)
        {
            SetValue(Data, Section, Key, Value.ToString());
            return true;
        }
        else
            return false;
    }

    internal static bool TrySet(string[] Data, string Section, string Key, double Value)
    {
        if (Value != double.Parse(GetValue(Data, Section, Key), CultureInfo.InvariantCulture))
        {
            SetValue(Data, Section, Key, $"{Value}");
            return true;
        }
        else
            return false;
    }

    internal static bool TrySet(string[] Data, string Section, string Key, int Value) => TrySet(Data, Section, Key, $"{Value}");

    internal static bool TrySet(ref string[] Data, string Section, string Key, IEnumerable<string> Values)
    {
        var Indices = FindValues(Data, Section, Key);
        var Index = FindSection(Data, Section);
        if (!Indices.Any())
        {
            if (Index is null)
            {
                Data = Data.Append($"[{Section}]").ToArray();
                Index = Data.Length - 1;
            }
            Append(ref Data, Index!.Value, Key, Values);
            return true;
        }
        else
        {
            var ROCopy = Data;
            if (Indices.Select(Index => GetValue(ROCopy, Index)).Order().SequenceEqual(Values.Order()))
                return false;
            else
            {
                Data = Data.Where((_, Index) => !Indices.Contains(Index)).ToArray();
                Append(ref Data, Index!.Value, Key, Values);
                return true;
            }
        }

        static void Append(ref string[] Data, int Index, string Key, IEnumerable<string> Values)
        {
            foreach (var Value in Values.Reverse())
                Data = Data[0..(Index + 1)].Append(Key + Separator + Value).Concat(Data[(Index + 1)..]).ToArray();
        }
    }

    internal static bool TryPrepend(ref string[] Data, string Section, string Key, string Value)
    {
        var Index = FindValues(Data, Section, Key)[0];
        if (Value != GetValue(Data, Index))
        {
            Data = Data[0..Index].Append(Key + Separator + Value).Concat(Data[Index..]).ToArray();
            return true;
        }
        return false;
    }

    internal static bool TryRemove(ref string[] Data, string Section)
    {
        var Index = FindSection(Data, Section);
        if (Index is not null)
        {
            while (Data.Length > Index && (Data[Index.Value].StartsWith($"[{Section}]") || !Data[Index.Value].StartsWith("[")))
                Data = Data[0..Index.Value].Concat(Data[(Index.Value + 1)..]).ToArray();
            return true;
        }
        return false;
    }

    internal static bool TryRemove(ref string[] Data, string Section, string Key)
    {
        var Values = FindValues(Data, Section, Key);
        if (Values.Any())
        {
            foreach (var Index in FindValues(Data, Section, Key))
                Data = Data[0..Index].Concat(Data[(Index + 1)..]).ToArray();
            return true;
        }
        else
            return false;
    }

    internal static bool TryRemove(ref string[] Data, string Section, string Key, string Value)
    {
        foreach (var Index in FindValues(Data, Section, Key))
            if (Value == GetValue(Data, Index))
            {
                Data = Data[0..Index].Concat(Data[(Index + 1)..]).ToArray();
                return true;
            }
        return false;
    }

    internal static string GetValue(string[] Config, string Section, string Key) => GetValue(Config, FindValue(Config, Section, Key));

    internal static string GetValue(string[] Config, int Index) => string.Join(Separator, Config[Index].Split(Separator)[1..]);

    internal static void SetValue(string[] Config, string Section, string Key, string Value) => Config[FindValue(Config, Section, Key)] = Key + Separator + Value;

    internal static int? FindSection(string[] Data, string Section)
    {
        var Result = 0;
        while ($"[{Section}]" != Data[Result])
            if (Result < Data.Length - 1)
                Result++;
            else
                return null;
        return Result;
    }

    internal static int FindValue(string[] Data, string Section, string Key)
    {
        var Result = 0;
        while ($"[{Section}]" != Data[Result])
            Result++;
        while (!Data[Result].StartsWith(Key))
            Result++;
        return Result;
    }

    internal static int[] FindValues(string[] Data, string Section, string Key)
    {
        var Result = Enumerable.Empty<int>();
        var Next = 0;
        while (Data.Length > Next && $"[{Section}]" != Data[Next])
            Next++;
        while (Data.Length - 1 > Next && !Data[Next + 1].StartsWith($"["))
        {
            Next++;
            if (Data[Next].StartsWith(Key))
                Result = Result.Append(Next);
        }
        return Result.ToArray();
    }

    internal static string Encode(IEnumerable<string> Maps) => $"(Maps=(\"{string.Join("\",\"", Maps)}\"))";

    internal static string GetValue(string Config, string Section, string Key) => GetValue(File.ReadAllLines(Config), Section, Key);

    const char Separator = '=';
}