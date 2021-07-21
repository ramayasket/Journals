using System;

namespace Journals.Api
{
    /// <summary>
    /// Overall parameters to the Journals application.
    /// The non-generic version is for the basic journal type.
    /// </summary>
    public abstract class Session
    {
        /// <summary>
        /// Describes application's output.
        /// </summary>
        public Column[] Columns;

        /// <summary>
        /// Number of records in application's output.
        /// Must be between 10 and 500.
        /// </summary>
        public int BufferSize;

        /// <summary>
        /// Describes how data is processed before being output.
        /// </summary>
        public Processor[] Processors;

        /// <summary>
        /// Reads journals out of log directory.
        /// </summary>
        public abstract void GetJournals(string directory, out Journal[] journals);
    }

    /// <summary>
    /// The generic version is for more complex journals.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Session<T> : Session where T:Journal
    {
        /// <summary>
        /// Transforms journal sequences.
        /// </summary>
        public Func<T[], T[]> Transform;

        /// <summary>
        /// Reads journals out of log directory.
        /// </summary>
        public abstract void GetJournals(string directory, out T[] journals);
    }
}
