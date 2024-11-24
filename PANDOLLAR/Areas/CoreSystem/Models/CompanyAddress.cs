using System;
using System.Collections.Generic;

namespace PANDOLLAR.Areas.CoreSystem.Models;

public partial class CompanyAddress
{
    public Guid AddressId { get; set; }

    public string Street { get; set; }

    public string City { get; set; }

    public string State { get; set; }

    public string PostalCode { get; set; }

    public string Country { get; set; }

    public virtual ICollection<Company> Companies { get; set; } = new List<Company>();
}
