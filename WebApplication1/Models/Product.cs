using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string Name { get; set; } = null!;

    public string? ActiveSubstance { get; set; }

    public string? Image { get; set; }

    public string? Form { get; set; }

    public string? Dosage { get; set; }

    public int? PrescriptionRequired { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<PharmacyStock> PharmacyStocks { get; set; } = new List<PharmacyStock>();
}
