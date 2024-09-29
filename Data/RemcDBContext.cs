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
        public DbSet<GeneratedForm> GeneratedForms { get; set; }
        public DbSet<FileRequirement> FileRequirement { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FundedResearchApplication>()
                .HasMany(f => f.GeneratedForms)
                .WithOne(g => g.FundedResearchApplication)
                .HasForeignKey(g => g.fra_Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FundedResearchApplication>()
                .HasMany(f => f.FileRequirements)
                .WithOne(g => g.fundedResearchApplication)
                .HasForeignKey(g => g.fra_Id)
                .OnDelete(DeleteBehavior.Cascade);

            base .OnModelCreating(modelBuilder);
        }
    }
}
