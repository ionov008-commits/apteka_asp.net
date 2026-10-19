using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class Customer
{
    public int CustomerId { get; set; }

    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string? DefaultAddress { get; set; }

    public virtual ICollection<CustomerOrder> CustomerOrders { get; set; } = new List<CustomerOrder>();

    public virtual User User { get; set; } = null!;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
