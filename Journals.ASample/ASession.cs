using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Journals.Api;

namespace Journals
{
    public class AJournal : Journal
    {
        public int Date;
    }

    public class AMaker : IJournalMaker<AJournal>
    {
        Regex _regex = new Regex("(?<token>[a-z-_]+)-(?<date>[0-9]+).log");

        public AJournal FromName(string name)
        {
            var match = _regex.Match(name);

            if (match.Success)
                return new AJournal { Path = name, Token = match.Groups["token"].Value, Date = int.Parse(match.Groups["date"].Value) };

            return null;
        }
    }

    public class AProcessor : ProcessorWithCutoff
    {
    }

    public abstract class ProcessorWithCutoff : Processor
    {
        public static readonly DateTime Cutoff = DateTime.Now;

                public override string ToString() => this.GetType().Name;

        public static bool IsAfterCutoff(FastSerilogLine line)
        {
            var t = DateTime.Parse(line["@t"]);
            return
                //t >= Cutoff;
                true;
        }
    }

    public class ASession : Session<AJournal>
    {
        public ASession()
        {
            Transform = new ATransform().Transform;

            Columns = new []
            {
                new Column(17, "@log"),
                new Column(8, "@t"),
                new Column(0, "SourceContext"),
                new Column(0, "@mt"),
                new Column(0, "@x"),
            };

            BufferSize = 100;

            Processors = new[]
            {
                new AProcessor
                {
                    Name = "AProcessor",
                    Process = x =>
                    {
                        if (ProcessorWithCutoff.IsAfterCutoff(x))
                        {
                            var at = x.At("@x");
                            // this is just a placeholder
                            return null == at ? null : x;
                        }

                        return null;
                    },
                }
            };
        }

        public override void GetJournals(string directory, out Journal[] journals)
        {
            GetJournals(directory, out var ajournals);
            journals = ajournals;
        }

        public override void GetJournals(string directory, out AJournal[] journals)
        {
            var maker = new AMaker();

            var files = Directory.EnumerateFiles(directory);

            journals = files.Select(maker.FromName).Where(x => null != x).ToArray();

            if (null != Transform)
                journals = Transform(journals);
        }
    }

    public class ATransform : ITransformJournals<AJournal>
    {
        public AJournal[] Transform(AJournal[] journals)
        {
            var transform = new Dictionary<string, List<AJournal>>();

            foreach (AJournal ajournal in journals)
            {
                if (ajournal != null)
                {
                    if (!transform.ContainsKey(ajournal.Token)) transform[ajournal.Token] = new List<AJournal>();

                    transform[ajournal.Token].Add(ajournal);
                }
            }

            var selected = transform.Values.Select(v => v.OrderBy(x => x.Date).Last()).ToArray();

            selected = selected/*.Where(x => x.Token == "marketdata-api")*/.ToArray();

            return selected;
        }
    }
}