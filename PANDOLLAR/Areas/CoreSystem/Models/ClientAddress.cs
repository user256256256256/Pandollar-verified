using System;
using System.Collections.Generic;

namespace PANDOLLAR.Areas.CoreSystem.Models;

public partial class ClientAddress
{
    public Guid AddressId { get; set; }

    public Guid ClientId { get; set; }

    public string Street { get; set; }

    public string City { get; set; }

    public string State { get; set; }

    public string PostalCode { get; set; }

    public string Country { get; set; }

    public virtual ICollection<CompanyClient> CompanyClients { get; set; } = new List<CompanyClient>();
}
