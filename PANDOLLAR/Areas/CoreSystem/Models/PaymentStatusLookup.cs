using System;
using System.Collections.Generic;

namespace PANDOLLAR.Areas.CoreSystem.Models;

public partial class PaymentStatusLookup
{
    public int Id { get; set; }

    public string Status { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
