namespace Journals.Api
{
    /// <summary>
    /// Preprocesses a list of journals (e.g. to select only the latests).
    /// </summary>
    public interface ITransformJournals<T> where T:Journal
    {
        T[] Transform(T[] journals);
    }
}