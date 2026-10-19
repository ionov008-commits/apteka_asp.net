using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class Pharmacy
{
    public int PharmacyId { get; set; }

    public int UserId { get; set; }

    public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string? Phone { get; set; }

    public string? DirectorName { get; set; }

    public string? WorkingHours { get; set; }

    public virtual ICollection<PharmacyStock> PharmacyStocks { get; set; } = new List<PharmacyStock>();

    public virtual User User { get; set; } = null!;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
