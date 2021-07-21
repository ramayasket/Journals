namespace Journals.Api
{
    /// <summary>
    /// A column in a journal's view.
    /// </summary>
    public class Column
    {
        public string Header { get; protected set; }
        public int Width { get; set; }

        public Column(int width, string header) => (Width, Header) = (width, header);
    }
}
