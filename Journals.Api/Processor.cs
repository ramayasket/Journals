using System;

namespace Journals.Api
{
    /// <summary>
    /// A journal processor.
    /// </summary>
    public class Processor
    {
        public string Name { get; set; }
        
        /// <summary>
        /// Filters and/or processes a journal record.
        /// </summary>
        public Func<FastSerilogLine, FastSerilogLine> Process { get; set; }

        public override string ToString() => Name;

        /// <summary>
        /// Reset internal data (e.g. aggregation, etc).
        /// </summary>
        public virtual void Reset()
        {
        }
    }
}
