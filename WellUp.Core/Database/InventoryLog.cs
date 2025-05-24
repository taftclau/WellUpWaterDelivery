using System;
using System.Collections.Generic;

namespace WellUp.Core.Database;

public partial class InventoryLog
{
    public int LogId { get; set; }

    public int ProductId { get; set; }

    public DateTime? LoggedAt { get; set; }

    public int PreviousQuantity { get; set; }

    public int NewQuantity { get; set; }

    public string ChangeReason { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
