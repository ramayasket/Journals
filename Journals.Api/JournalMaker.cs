using System.IO;

namespace Journals.Api
{
    /// <summary>
    /// The default journal maker. Uses the file name without extension as a token.
    /// For more complex file names, derive a class from this one.
    /// </summary>
    public class JournalMaker : IJournalMaker<Journal>
    {
        public Journal FromName(string name) =>
            new Journal
            {
                Path = name,
                Token = Path.GetFileNameWithoutExtension(name)
            };
    }
}
