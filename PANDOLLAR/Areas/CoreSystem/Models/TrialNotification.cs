using System;
using System.Collections.Generic;

namespace PANDOLLAR.Areas.CoreSystem.Models;

public partial class TrialNotification
{
    public Guid Id { get; set; }

    public Guid? CompanyId { get; set; }

    public DateTime TrialStartDate { get; set; }

    public DateTime TrialEndDate { get; set; }

    public bool IsNotified { get; set; }

    public DateTime? ReminderDate { get; set; }

    public DateTime? SentAt { get; set; }

    public int NotificationTypeId { get; set; }

    public virtual Company Company { get; set; }

    public virtual TrialNotificationLookup NotificationType { get; set; }
}
