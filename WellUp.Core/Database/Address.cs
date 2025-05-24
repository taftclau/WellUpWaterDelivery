using System;
using System.Collections.Generic;

namespace WellUp.Core.Database;

public partial class Address
{
    public int AddressId { get; set; }

    public int CustomerId { get; set; }

    public string CityMunicipality { get; set; } = null!;

    public string Barangay { get; set; } = null!;

    public string Street { get; set; } = null!;

    public string ZipCode { get; set; } = null!;

    public bool IsDefault { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
