using System;
using System.Collections.Generic;

namespace PANDOLLAR.Areas.CoreSystem.Models;

public partial class Feedback
{
    public Guid FeedbackId { get; set; }

    public string UserId { get; set; }

    public string FeedbackText { get; set; }

    public int? Rating { get; set; }

    public string Category { get; set; }

    public DateTime SubmittedAt { get; set; }

    public bool Resolved { get; set; }

    public Guid CompanyId { get; set; }

    public virtual Company Company { get; set; }

    public virtual AspNetUser User { get; set; }
}
