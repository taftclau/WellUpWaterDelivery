using System;
using System.Collections.Generic;

namespace WellUp.Core.Database;

public partial class Product
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public string? Description { get; set; }

    public string? ContainerType { get; set; }

    public bool IncludesRefill { get; set; }

    public decimal Price { get; set; }

    public int? StockQuantity { get; set; }

    public int? LowStockThreshold { get; set; }

    public bool? IsAvailable { get; set; }

    public virtual ICollection<InventoryLog> InventoryLogs { get; set; } = new List<InventoryLog>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public string? ImagePath { get; set; }
}
