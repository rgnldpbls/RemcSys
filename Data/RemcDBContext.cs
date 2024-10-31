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
        public DbSet<FundedResearchEthics> FundedResearchEthics { get; set; }
        public DbSet<ActionLog> ActionLogs { get; set; }
        public DbSet<Evaluation> Evaluations { get; set; }
        public DbSet<Evaluator> Evaluator { get; set; }
        public DbSet<FundedResearch> FundedResearches { get; set; }
        public DbSet<ProgressReport> ProgressReports { get; set; }
        public DbSet<GenerateReport> GenerateReports { get; set; }
        public DbSet<GenerateGAWADNominees> GenerateGAWADNominees { get; set; }
        public DbSet<GAWADWinners> GAWADWinners { get; set; }
        public DbSet<CalendarEvent> CalendarEvents { get; set; }
        public DbSet<Settings> Settings { get; set; }
        public DbSet<Guidelines> Guidelines { get; set; }
        
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

            modelBuilder.Entity<FundedResearchApplication>()
                .HasOne(f => f.FundedResearchEthics)
                .WithOne(g => g.FundedResearchApplication)
                .HasForeignKey<FundedResearchEthics>(g => g.fra_Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FundedResearchApplication>()
                .HasMany(f => f.ActionLogs)
                .WithOne(g => g.fundedResearchApplication)
                .HasForeignKey(g => g.FraId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FundedResearchApplication>()
                .HasMany(f => f.Evaluations)
                .WithOne(g => g.fundedResearchApplication)
                .HasForeignKey(g => g.fra_Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Evaluator>()
                .HasMany(e => e.Evaluations)
                .WithOne(g => g.evaluator)
                .HasForeignKey(g => g.evaluator_Id);

            modelBuilder.Entity<FundedResearch>()
                .HasOne(f => f.FundedResearchApplication)
                .WithOne(f => f.FundedResearch)
                .HasForeignKey<FundedResearch>(g => g.fra_Id);

            modelBuilder.Entity<FundedResearch>()
                .HasMany(f => f.ProgressReports)
                .WithOne(f => f.FundedResearch)
                .HasForeignKey(g => g.fr_Id);

            base .OnModelCreating(modelBuilder);
        }
    }
}
