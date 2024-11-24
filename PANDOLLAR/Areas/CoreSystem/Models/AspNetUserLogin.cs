using System;
using System.Collections.Generic;

namespace PANDOLLAR.Areas.CoreSystem.Models;

public partial class AspNetUserLogin
{
    public string LoginProvider { get; set; }

    public string ProviderKey { get; set; }

    public string ProviderDisplayName { get; set; }

    public string UserId { get; set; }

    public virtual AspNetUser User { get; set; }
}
