using System;
using System.Collections.Generic;

namespace PANDOLLAR.Areas.CoreSystem.Models;

public partial class Company
{
    public Guid CompanyId { get; set; }

    public string CompanyName { get; set; }

    public string ContactPerson { get; set; }

    public string CompanyEmail { get; set; }

    public string CompanyPhone { get; set; }

    public DateTime? ExpDate { get; set; }

    public string ApiCode { get; set; }

    public string CompanyStatus { get; set; }

    public decimal? SubscriptionAmount { get; set; }

    public string CompanyInitials { get; set; }

    public string SmsAccount { get; set; }

    public string PayAccount { get; set; }

    public string PayBank { get; set; }

    public string PayAccountName { get; set; }

    public string Motto { get; set; }

    public string CompanyType { get; set; }

    public Guid AddressId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string CompanyLogo { get; set; }

    public string CompanyLogoFilePath { get; set; }

    public virtual CompanyAddress Address { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<CompanyClient> CompanyClients { get; set; } = new List<CompanyClient>();

    public virtual ICollection<DataMigration> DataMigrations { get; set; } = new List<DataMigration>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}
