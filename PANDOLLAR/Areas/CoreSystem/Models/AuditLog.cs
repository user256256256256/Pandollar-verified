using System;
using System.Collections.Generic;

namespace PANDOLLAR.Areas.CoreSystem.Models;

public partial class AuditLog
{
    public Guid AuditLogId { get; set; }

    public string UserId { get; set; }

    public string Action { get; set; }

    public DateTime Timestamp { get; set; }

    public string Details { get; set; }

    public string IpAddress { get; set; }

    public string DeviceInfo { get; set; }

    public string EventType { get; set; }

    public string EntityAffected { get; set; }

    public string OldValue { get; set; }

    public string NewValue { get; set; }

    public string ComplianceStatus { get; set; }

    public Guid CompanyId { get; set; }

    public virtual Company Company { get; set; }

    public virtual AspNetUser User { get; set; }
}
