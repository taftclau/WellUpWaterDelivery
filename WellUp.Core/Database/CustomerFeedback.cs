using System;
using System.Collections.Generic;

namespace WellUp.Core.Database;

public partial class CustomerFeedback
{
    public int FeedbackId { get; set; }

    public int CustomerId { get; set; }

    public int OrderId { get; set; }

    public string FeedbackType { get; set; } = null!;

    public string Description { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
