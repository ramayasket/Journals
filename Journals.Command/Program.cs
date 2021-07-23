using System;
using System.CodeDom.Compiler;
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
    /// <summary> Άρχων κάνων </summary>
    /// <remarks> https://ru.wikipedia.org/wiki/Канонарх </remarks>
    class Program
    {
        static void AcceptCommandLine(string[] args, out string directory, out string assembly, out string processor)
        {
            directory = null;
            assembly = null;
            processor = null;

            // using System.CodeDom.Compiler;
            CodeDomProvider provider = CodeDomProvider.CreateProvider("C#");

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
                else if (provider.IsValidIdentifier(param))
                {
                    processor = param;
                    Console.WriteLine($"processor_name is {processor}");
                }
            }
        }

        const string COMMAND = "Journals.Command";

        static int Main(string[] args)
        {
            var executing = Assembly.GetExecutingAssembly();
            var name = executing.GetName();
            var version = name.Version;

            Console.WriteLine($"{COMMAND} {version}");

            AcceptCommandLine(args, out string directory, out string assembly, out string processorName);

            directory ??= Environment.GetEnvironmentVariable("JOURNALS_PATH");
            directory ??= Environment.CurrentDirectory;

            if (null == directory || null == assembly)
            {
                Console.WriteLine($"Usage: {COMMAND} [log_directory_path\\] session_assembly_path.dll [processor_name]");
                return 1;
            }

            try
            {
                using (var stream = File.OpenRead(assembly))
                {
                    Session = SessionLoader.LoadSession(stream);

                    if (null == Session)
                    {
                        Console.WriteLine($"Couldn't find any session(s) in the assembly {assembly}.");
                        return 1;
                    }
                }
            }
            catch (Exception x)
            {
                Console.WriteLine($"Couldn't read from the assembly file {assembly}.");
                return 1;
            }

            Session.GetJournals(directory, out var journals);

            IQueryable<Processor> processors = Session.Processors.AsQueryable();

            if (null != processorName)
                processors = processors.Where(x => x.ToString() == processorName);
                
            Processor = processors.FirstOrDefault();

            var buffer = new FlyingBuffer(100) { Processor = Processor, Added = OnLine };
            var readers = journals.Select(x => new FlyingReader(x, buffer)).ToArray();

            while(true) Thread.Sleep(100);
        }

        static Session Session;
        private static Processor Processor;

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

            var att = line.At<DateTime>("@t");

            var acc = new StringBuilder();

            for (int i = 0; i < Processor.Columns.Length; i++)
            {
                var column = Processor.Columns[i];

                var f = line[column.Header] ?? "";

                if(0 != column.Width)
                    f = NormalizeLength(f, column.Width);

                acc.Append(f);

                if (Processor.Columns.Length - 1 > i)
                    acc.Append(" ");
            }

            var s = acc.ToString();

            Console.WriteLine(s);
        }
    }
}
