using PANDOLLAR.Areas.CoreSystem.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PANDOLLAR.Models;

public partial class CompanyClient
{
    [Key]
    public Guid ClientId { get; set; }

    public Guid CompanyId { get; set; }

    public string ClientName { get; set; }

    public DateTime DateOfBirth { get; set; }

    public string Gender { get; set; }

    public string Email { get; set; }

    public string PhoneNumber { get; set; }

    public Guid AddressId { get; set; }

    public string EmergencyContactName { get; set; }

    public string EmergencyContactPhone { get; set; }

    public string MaritalStatus { get; set; }

    public string Nationality { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ClientAddress Address { get; set; }

    public virtual Company Company { get; set; }

}
