using System;
using System.Collections.Generic;

namespace PANDOLLAR.Areas.CoreSystem.Models;

public partial class SubscriptionPlan
{
    public Guid Id { get; set; }

    public int PlanNameId { get; set; }

    public string Description { get; set; }

    public int Duration { get; set; }

    public int BillingCycleId { get; set; }

    public virtual BillingCycleLookup BillingCycle { get; set; }

    public virtual SubscriptionPlanNameLookup PlanName { get; set; }

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
