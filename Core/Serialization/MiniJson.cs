using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace VrAction.Core.Serialization
{
    /// <summary>Minimal JSON reader (objects, arrays, strings, numbers, bool, null). Numbers stay as raw text.</summary>
    public static class MiniJson
    {
        public sealed class Number
        {
            public string Raw { get; }
            public Number(string raw) { Raw = raw; }
        }

        public static object Parse(string json)
        {
            int i = 0;
            var v = ParseValue(json, ref i);
            SkipWs(json, ref i);
            if (i != json.Length) throw new FormatException("trailing characters in json");
            return v;
        }

        static void SkipWs(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }

        static object ParseValue(string s, ref int i)
        {
            SkipWs(s, ref i);
            if (i >= s.Length) throw new FormatException("unexpected end of json");
            char c = s[i];
            if (c == '{') return ParseObject(s, ref i);
            if (c == '[') return ParseArray(s, ref i);
            if (c == '"') return ParseString(s, ref i);
            if (c == 't' && string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
            if (c == 'f' && string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
            if (c == 'n' && string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }
            int start = i;
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '-' || s[i] == '+' || s[i] == '.' || s[i] == 'e' || s[i] == 'E')) i++;
            if (start == i) throw new FormatException("unexpected character '" + c + "'");
            return new Number(s.Substring(start, i - start));
        }

        static Dictionary<string, object> ParseObject(string s, ref int i)
        {
            var d = new Dictionary<string, object>();
            i++; SkipWs(s, ref i);
            if (s[i] == '}') { i++; return d; }
            while (true)
            {
                SkipWs(s, ref i);
                string key = ParseString(s, ref i);
                SkipWs(s, ref i);
                if (s[i] != ':') throw new FormatException("expected ':'");
                i++;
                d[key] = ParseValue(s, ref i);
                SkipWs(s, ref i);
                if (s[i] == ',') { i++; continue; }
                if (s[i] == '}') { i++; return d; }
                throw new FormatException("expected ',' or '}'");
            }
        }

        static List<object> ParseArray(string s, ref int i)
        {
            var l = new List<object>();
            i++; SkipWs(s, ref i);
            if (s[i] == ']') { i++; return l; }
            while (true)
            {
                l.Add(ParseValue(s, ref i));
                SkipWs(s, ref i);
                if (s[i] == ',') { i++; continue; }
                if (s[i] == ']') { i++; return l; }
                throw new FormatException("expected ',' or ']'");
            }
        }

        static string ParseString(string s, ref int i)
        {
            if (s[i] != '"') throw new FormatException("expected string");
            i++;
            var sb = new StringBuilder();
            while (i < s.Length && s[i] != '"')
            {
                if (s[i] == '\\')
                {
                    i++;
                    switch (s[i])
                    {
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': sb.Append('\r'); break;
                        case 'u': sb.Append((char)Convert.ToInt32(s.Substring(i + 1, 4), 16)); i += 4; break;
                        default: sb.Append(s[i]); break;
                    }
                }
                else sb.Append(s[i]);
                i++;
            }
            if (i >= s.Length) throw new FormatException("unterminated string");
            i++;
            return sb.ToString();
        }

        // ---- typed accessors -------------------------------------------------
        public static Dictionary<string, object> Obj(object o)
        {
            if (o is Dictionary<string, object> d) return d;
            throw new FormatException("expected object");
        }

        public static object Get(Dictionary<string, object> d, string key)
        {
            if (!d.TryGetValue(key, out var v)) throw new FormatException("missing field '" + key + "'");
            return v;
        }

        public static long Long(object o)
        {
            if (o is Number n) return long.Parse(n.Raw, CultureInfo.InvariantCulture);
            throw new FormatException("expected number");
        }

        public static ulong ULong(object o)
        {
            if (o is Number n) return ulong.Parse(n.Raw, CultureInfo.InvariantCulture);
            throw new FormatException("expected number");
        }

        public static string Str(object o)
        {
            if (o is string s) return s;
            throw new FormatException("expected string");
        }

        public static List<object> Arr(object o)
        {
            if (o is List<object> l) return l;
            throw new FormatException("expected array");
        }

        public static string Quote(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (char c in s)
            {
                if (c == '"' || c == '\\') sb.Append('\\').Append(c);
                else if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                else sb.Append(c);
            }
            return sb.Append('"').ToString();
        }
    }
}
