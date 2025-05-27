using System;
using System.Collections.Generic;

namespace WellUp.Core.Database;

public partial class Customer
{
    public int CustomerId { get; set; }

    public string CustomerEmail { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public DateTime? DateCreated { get; set; }

    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();

    public virtual ICollection<CustomerDetail> CustomerDetails { get; set; } = new List<CustomerDetail>();

    public virtual ICollection<CustomerFeedback> CustomerFeedbacks { get; set; } = new List<CustomerFeedback>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

}
