namespace PANDOLLAR.Areas.CoreSystem.Models
{
    public class CompanyStatusLookup
    {
        public int StatusId { get; set; } 
        public string StatusName { get; set; }

        public virtual ICollection<Company> Companies { get; set; } = new List<Company>();
    }
}
