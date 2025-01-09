using System;
using System.Collections.Generic;

namespace PANDOLLAR.Areas.CoreSystem.Models;

public partial class SubscriptionActivityLookup
{
    public int Id { get; set; }

    public string ActivityName { get; set; }

    public virtual ICollection<SubscriptionLog> SubscriptionLogs { get; set; } = new List<SubscriptionLog>();
}
