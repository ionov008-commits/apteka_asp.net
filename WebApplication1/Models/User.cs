using System;
using System.Collections.Generic;

namespace WebApplication1.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Password { get; set; } = null!;

    public string Role { get; set; } = null!;
    public string Username { get; set; }
    public string Email { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Pharmacy? Pharmacy { get; set; }
}
