using System;
using System.Collections.Generic;

namespace PANDOLLAR.Areas.CoreSystem.Models;

public partial class SubscriptionLog
{
    public Guid Id { get; set; }

    public Guid SubscriptionId { get; set; }

    public int ActivityId { get; set; }

    public DateTime LogDate { get; set; }

    public virtual SubscriptionActivityLookup Activity { get; set; }

    public virtual Subscription Subscription { get; set; }
}
