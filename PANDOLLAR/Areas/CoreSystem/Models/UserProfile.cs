using System;
using System.Collections.Generic;

namespace PANDOLLAR.Areas.CoreSystem.Models;

public partial class UserProfile
{
    public Guid UserProfileId { get; set; }

    public string UserId { get; set; }

    public string ProfileImagePath { get; set; }

    public string UserBio { get; set; }

    public virtual AspNetUser User { get; set; }
}
