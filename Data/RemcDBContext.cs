using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RemcSys.Models;

namespace RemcSys.Data
{
    public class RemcDBContext : DbContext
    {
        public RemcDBContext (DbContextOptions<RemcDBContext> options)
            : base(options)
        {
        }

        public DbSet<FundedResearchApplication> FundedResearchApplication { get; set; }
        public DbSet<ResearchStaff> ResearchStaff { get; set; }
        public DbSet<FileRequirement> FileRequirement { get; set; }
    }
}
