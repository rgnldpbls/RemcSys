using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Build.Framework;

namespace RemcSys.Areas.Identity.Data;

// Add profile data for application users by adding properties to the SystemUser class
public class SystemUser : IdentityUser
{
    [Required]
    public string? Name { get; set; }
    [Required]
    public string? College {  get; set; }
    [Required]
    public string? Branch { get; set; }
}

