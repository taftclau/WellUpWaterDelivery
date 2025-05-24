using System;
using System.Collections.Generic;

namespace WellUp.Core.Database;

public partial class CustomerDetail
{
    public int CustomerDetailsId { get; set; }

    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;
}
