using FreelaHunter.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace FreelaHunter.Infrastructure.Persistence;

public sealed class FreelaHunterDbContext : DbContext
{
    public DbSet<OpportunityRecord> Opportunities => Set<OpportunityRecord>();
    public DbSet<OpportunitySkillRecord> OpportunitySkills => Set<OpportunitySkillRecord>();
    public DbSet<FreelancerProfileRecord> FreelancerProfiles => Set<FreelancerProfileRecord>();
    public DbSet<FreelancerProfileSkillRecord> FreelancerProfileSkills => Set<FreelancerProfileSkillRecord>();
    public DbSet<FreelancerHourlyRateRecord> FreelancerHourlyRates => Set<FreelancerHourlyRateRecord>();
    public DbSet<OpportunityEvaluationSnapshotRecord> OpportunityEvaluationSnapshots => Set<OpportunityEvaluationSnapshotRecord>();

    public FreelaHunterDbContext(DbContextOptions<FreelaHunterDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OpportunityRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Marketplace).HasMaxLength(50);
            e.Property(x => x.IntakeChannel).HasMaxLength(50);
            e.Property(x => x.Title).HasMaxLength(300);
            e.Property(x => x.BudgetKind).HasMaxLength(50);
            e.Property(x => x.BudgetBasis).HasMaxLength(50);
            e.Property(x => x.BudgetCurrency).HasMaxLength(10);
            e.Property(x => x.Status).HasMaxLength(50);
            e.HasIndex(x => x.CapturedAtUtc);
            e.HasIndex(x => x.PublishedAtUtc);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.Marketplace);
        });

        modelBuilder.Entity<OpportunitySkillRecord>(e =>
        {
            e.HasKey(x => new { x.OpportunityId, x.Skill });
            e.HasOne(x => x.Opportunity)
                .WithMany(o => o.Skills)
                .HasForeignKey(x => x.OpportunityId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Skill).HasMaxLength(200);
        });

        modelBuilder.Entity<FreelancerProfileRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.DefaultRateCurrency).HasMaxLength(10);
        });

        modelBuilder.Entity<FreelancerProfileSkillRecord>(e =>
        {
            e.HasKey(x => new { x.FreelancerProfileId, x.Name });
            e.HasOne(x => x.FreelancerProfile)
                .WithMany(p => p.Skills)
                .HasForeignKey(x => x.FreelancerProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Level).HasMaxLength(50);
        });

        modelBuilder.Entity<FreelancerHourlyRateRecord>(e =>
        {
            e.HasKey(x => new { x.FreelancerProfileId, x.Currency });
            e.HasOne(x => x.FreelancerProfile)
                .WithMany(p => p.HourlyRates)
                .HasForeignKey(x => x.FreelancerProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Currency).HasMaxLength(10);
        });

        modelBuilder.Entity<OpportunityEvaluationSnapshotRecord>(e =>
        {
            e.HasKey(x => x.OpportunityId);
            e.HasOne(x => x.Opportunity)
                .WithMany()
                .HasForeignKey(x => x.OpportunityId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.ProfileFingerprint).HasMaxLength(64);
            e.Property(x => x.Recommendation).HasMaxLength(50);
            e.Property(x => x.PayloadJson).HasMaxLength(50000);
            e.HasIndex(x => x.EvaluatedAtUtc);
            e.HasIndex(x => x.Recommendation);
            e.HasIndex(x => x.OverallScore);
        });
    }
}
