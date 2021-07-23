using System;

namespace Journals.Api
{
    /// <summary>
    /// A journal processor.
    /// </summary>
    public abstract class Processor
    {
        public Session Session { get; }

        public override string ToString() => GetType().Name;

        protected Processor(Session session)
        {
            Session = session;
        }

        /// <summary>
        /// Describes application's output.
        /// </summary>
        public abstract Column[] Columns { get; }

        /// <summary>
        /// Filters and/or processes a journal record.
        /// </summary>
        public abstract FastSerilogLine Process(FastSerilogLine line);

        /// <summary>
        /// Reset internal data (e.g. aggregation, etc).
        /// </summary>
        public virtual void Reset()
        {
        }
    }

    public class NullProcessor : Processor
    {
        public NullProcessor() : base(null) { }
        
        public NullProcessor(Session session) : base(session) { }

        public override string ToString() => "*";

        public override Column[] Columns => Session?.Columns ?? new []
        {
            new Column(0, "@mt"), 
        };

        public override FastSerilogLine Process(FastSerilogLine line) => line;
    }
}
