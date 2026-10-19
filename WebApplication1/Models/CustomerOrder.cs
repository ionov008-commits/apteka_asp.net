using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class CustomerOrder
{
    public int OrderId { get; set; }

    public int CustomerId { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public decimal? TotalCost { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
