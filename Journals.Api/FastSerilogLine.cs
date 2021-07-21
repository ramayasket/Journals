using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;

namespace Journals.Api
{
    /// <summary>
    /// A journal record in JSON format.
    /// </summary>
    public class FastSerilogLine
    {
        public long Number;

        readonly Dictionary<string, string> _fields = new Dictionary<string, string>();

        /// <summary>
        /// Returns field names.
        /// </summary>
        public string[] Fields => _fields.Keys.ToArray();

        public string this[string name]
        {
            get => _fields.GetValueOrDefault(name);
            set => _fields[name] = value;
        }

        /// <summary>
        /// Returns a field value.
        /// </summary>
        /// <param name="name">Field name.</param>
        /// <returns>Field value.</returns>
        public string At(string name) => this[name];

        unsafe string[] ParseJsonFast(string line)
        {
            var parts = new AList();
            var length = line.Length;

            fixed (char* chars = &(line.ToCharArray()[0]))
            {
                ParsePrologue(chars, parts, length);
            }

            return parts.ToArray();
        }

        class AList : List<string>
        {
            public new void Add(string s)
            {
                base.Add(s);
            }
        }

        unsafe void ParsePrologue(char* chars, AList parts, int length)
        {
            int ix = 0;

            Debug.Assert(chars[ix] == '{');
            ix++;

            ParseName(chars, ref ix, parts, length);
        }

        unsafe void ParseName(char* chars, ref int ix, AList parts, int length)
        {
            if(chars[ix++] != '\"')
                Debug.Assert(false);

            var ix0 = ix;

            while (chars[ix++] != '\"') ;

            var name = new string(chars, ix0, ix - ix0 - 1);
            parts.Add(name);

            ParseValue(chars, ref ix, parts, length);
        }

        unsafe void ParseValue(char* chars, ref int ix, AList parts, int length)
        {
            if(chars[ix++] != ':')
                Debug.Assert(false);

            if (chars[ix] == '\"')
            {
                ix ++;
                ParseStringValue(chars, ref ix, parts, length);
            }

            else
            {
                ParseNonStringValue(chars, ref ix, parts, length);
            }
        }

        unsafe void ParseNonStringValue(char* chars, ref int ix, AList parts, int length)
        {
            var ix0 = ix;

            if (chars[ix] == '{')
            {
                int inside = 1;
                ix ++;

                while (inside != 0)
                {
                    if (chars[ix] == '{')
                        inside ++;

                    if (chars[ix] == '}')
                        inside --;

                    ix ++;
                }
            }
            else if (chars[ix] == '[')
            {
                int inside = 1;
                ix++;

                while (inside != 0)
                {
                    if (chars[ix] == '[')
                        inside++;

                    if (chars[ix] == ']')
                        inside--;

                    ix++;
                }
            }
            else
                while (chars[ix] != ',' && chars[ix] != '}') ix++ ;

            var value = new string(chars, ix0, ix - ix0);
            parts.Add(value);

            if (chars[ix] == ',')
            {
                ix ++;
                ParseName(chars, ref ix, parts, length);
            }

            else if (chars[ix] == '}')
                return;

            if (ix >= length - 1)
                return;

            throw new InvalidOperationException("Unexpected character after a value");
        }

        unsafe void ParseHtml(char* chars, ref int ix, AList parts, int length)
        {
            while (true)
            {
                var s = new string(chars, ix, 7);
                if (s.Equals("</HTML>", StringComparison.OrdinalIgnoreCase))
                {
                    ix += 7;
                    break;
                }
            }
        }

        unsafe void ParseStringValue(char* chars, ref int ix, AList parts, int length)
        {
            var ix0 = ix;

            while (true)
            {
                var s = new string(chars, ix, 14);
                if (s.Equals("<!DOCTYPE HTML", StringComparison.OrdinalIgnoreCase))
                {
                    ParseHtml(chars, ref ix, parts, length);
                }
                else
                {
                    var c = chars[ix++];
                    if (c == '\"')
                        break;
                }
            }

            var value = new string(chars, ix0, ix - ix0 - 1);
            parts.Add(value);

            if (chars[ix] == ',')
            {
                ix ++;
                ParseName(chars, ref ix, parts, length);
            }

            else if (chars[ix] == '}')
                return;

            if (ix >= length-1)
                return;

            throw new InvalidOperationException("Unexpected character after a value");
        }

        /// <summary>
        /// Construct a record from a flat string.
        /// </summary>
        /// <param name="line">Flat string.</param>
        public FastSerilogLine(string line)
        {
            var parts = ParseJsonFast(line);

            Debug.Assert(parts.Length % 2 == 0);

            int ix = 0;
            while (ix < parts.Length)
            {
                var name = parts[ix++];
                var field = parts[ix++];

                _fields[name] = field;
            }

            Actualize();
        }

        /// <summary>
        /// Replaces field names (i.e. {field}) with actual values.
        /// </summary>
        void Actualize()
        {
            foreach (var p in Fields) // p - шаблон замены
            {
                foreach (var s in Fields) // s - где меняем
                {
                    var pattern = $"{{{p}}}";
                    var value = this[p];

                    var data = this[s];

                    if (p != s && data.Contains(pattern))
                    {
                        var newValue = data.Replace(pattern, value);
                        this[s] = newValue;
                    }
                }
            }
        }
    }
}
