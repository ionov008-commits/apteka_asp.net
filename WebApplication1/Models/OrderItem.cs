using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class OrderItem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int StockId { get; set; }

    public int Quantity { get; set; }

    public virtual CustomerOrder Order { get; set; } = null!;

    public virtual PharmacyStock Stock { get; set; } = null!;
    public string Status { get; set; } = "Pending";
}
