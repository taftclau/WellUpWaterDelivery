using System;
using System.Collections.Generic;

namespace WellUp.Core.Database;

public partial class Delivery
{
    public int DeliveryId { get; set; }

    public int OrderId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime ScheduledTime { get; set; }

    public string Notes { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
