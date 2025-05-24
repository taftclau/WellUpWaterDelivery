using System;
using System.Collections.Generic;

namespace WellUp.Core.Database;

public partial class Admin
{
    public int AdminId { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string AdminName { get; set; } = null!;

    public DateTime? DateModified { get; set; }
}
