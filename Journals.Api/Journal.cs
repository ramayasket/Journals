namespace Journals.Api
{
    /// <summary>
    /// Base class for journals.
    /// </summary>
    public class Journal
    {
        /// <summary>
        /// Kind of journal.
        /// </summary>
        public string Token;
        
        /// <summary>
        /// Full path to the journal's file.
        /// </summary>
        public string Path;
    }
}