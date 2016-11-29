namespace Minerva.Importer
{
    public class DanishCompanyIndex
    {
        public string CompanyName { get; set; }
        public string CoName { get; set; }
        public string Street { get; set; }
        public string Zip { get; set; }
        public string City { get; set; }
        public string PlaceName { get; set; }
        public string CVRNumber { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Id => CVRNumber;
    }
}