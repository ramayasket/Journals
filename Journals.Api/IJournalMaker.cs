namespace Journals.Api
{
    /// <summary>
    /// Produces a journal out of a file name.
    /// </summary>
    public interface IJournalMaker<out T> where T:Journal
    {
        T FromName(string name);
    }
}