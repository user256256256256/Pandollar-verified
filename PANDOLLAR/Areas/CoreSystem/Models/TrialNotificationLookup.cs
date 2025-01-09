using System;
using System.Collections.Generic;

namespace PANDOLLAR.Areas.CoreSystem.Models;

public partial class TrialNotificationLookup
{
    public int Id { get; set; }

    public string Type { get; set; }

    public string Message { get; set; }

    public virtual ICollection<TrialNotification> TrialNotifications { get; set; } = new List<TrialNotification>();
}
