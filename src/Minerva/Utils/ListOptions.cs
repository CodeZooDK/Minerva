namespace Minerva.Utils
{
    public class ListOptions
    {
        public ListOptions()
        {
            Page = 0;
            PageSize = 10;
        }

        public bool ShowDeactivated { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool ForceNoPaging { get; set; }
    }
}