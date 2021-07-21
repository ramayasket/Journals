using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Journals.Api;
using Watch.API;

namespace Journals.Command
{
    class Program
    {
        static void AcceptCommandLine(string[] args, out string directory, out string assembly)
        {
            directory = null;
            assembly = null;

            for (int i = 0; i < args.Length; i++)
            {
                var param = args[i];

                if (Directory.Exists(param))
                {
                    if (null != directory)
                    {
                        directory = null;
                        break;
                    }

                    directory = param;
                    Console.WriteLine($"log_directory_path is {directory}");
                }
                else if (File.Exists(param))
                {
                    if (null != assembly)
                    {
                        assembly = null;
                        break;
                    }

                    var ext = Path.GetExtension(param);

                    if (".dll" == ext)
                    {
                        assembly = param;
                        Console.WriteLine($"session_assembly_path is {assembly}");
                    }
                }
            }
        }

        const string COMMAND = "Journals.Command";

        static void Main(string[] args)
        {
            var executing = Assembly.GetExecutingAssembly();
            var name = executing.GetName();
            var version = name.Version;

            Console.WriteLine($"{COMMAND} {version}");

            AcceptCommandLine(args, out string directory, out string assembly);

            directory ??= Environment.GetEnvironmentVariable("JOURNALS_PATH");

            if (null == directory || null == assembly)
            {
                Console.WriteLine($"Usage: {COMMAND} log_directory_path\\ session_assembly_path.dll");
                return;
            }

            try
            {
                using (var stream = File.OpenRead(assembly))
                {
                    Session = SessionLoader.LoadSession(stream);

                    if (null == Session)
                    {
                        Console.WriteLine($"Couldn't find any session(s) in the assembly {assembly}.");
                        return;
                    }
                }
            }
            catch (Exception x)
            {
                Console.WriteLine($"Couldn't read from the assembly file {assembly}.");
                return;
            }

            Session.GetJournals(directory, out var journals);

            var processor = Session.Processors.FirstOrDefault() ?? new Processor { Name = "*", Process = x => x };

            var buffer = new FlyingBuffer(100) { Processor = processor, Added = OnLine };
            var readers = journals.Select(x => new FlyingReader(x, buffer)).ToArray();

            while(true) Thread.Sleep(100);
        }

        static Session Session;

        static string NormalizeLength(string s, int width)
        {
            if(s.Length > width)
                s = s.Substring(0, width);

            s = s.PadRight(width, ' ');
            return s;
        }

        static void WithColumn(FastSerilogLine line, string header, Func<string, string> deed)
        {
            var t = line[header];

            if (null != t)
            {
                line[header] = deed(t);
            }
        }

        static string NormalizeLineBreaksEtc(string s) => s
            .Replace("\\r\\n", Environment.NewLine)
            .Replace("\\n", Environment.NewLine)
            .Replace("\\t", " ");

        static void OnLine(FastSerilogLine line)
        {
            WithColumn(line, "@t", t => DateTime.Parse(t).ToString("T"));
            WithColumn(line, "@mt", NormalizeLineBreaksEtc);
            WithColumn(line, "@x", NormalizeLineBreaksEtc);

            var acc = new StringBuilder();

            for (int i = 0; i < Session.Columns.Length; i++)
            {
                var column = Session.Columns[i];

                var f = line[column.Header] ?? "";

                if(0 != column.Width)
                    f = NormalizeLength(f, column.Width);

                acc.Append(f);

                if (Session.Columns.Length - 1 > i)
                    acc.Append(" ");
            }

            var s = acc.ToString();

            Console.WriteLine(s);
        }
    }
}
