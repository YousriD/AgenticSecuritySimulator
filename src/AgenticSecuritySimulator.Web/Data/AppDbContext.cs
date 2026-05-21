using AgenticSecuritySimulator.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgenticSecuritySimulator.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Twin> Twins => Set<Twin>();
    public DbSet<TwinNode> Nodes => Set<TwinNode>();
    public DbSet<TwinEdge> Edges => Set<TwinEdge>();
    public DbSet<AttackScenario> AttackScenarios => Set<AttackScenario>();
    public DbSet<SimulationBatch> SimulationBatches => Set<SimulationBatch>();
    public DbSet<SimulationRun> SimulationRuns => Set<SimulationRun>();
    public DbSet<SimulationEvent> SimulationEvents => Set<SimulationEvent>();
    public DbSet<ResilienceScoreDetail> ResilienceScores => Set<ResilienceScoreDetail>();
    public DbSet<BatchStatistics> BatchStatistics => Set<BatchStatistics>();
    public DbSet<AiProviderSetting> AiProviderSettings => Set<AiProviderSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>().ToTable("Organizations");
        modelBuilder.Entity<Twin>().ToTable("Twins");
        modelBuilder.Entity<TwinNode>().ToTable("Nodes");
        modelBuilder.Entity<TwinEdge>().ToTable("Edges");
        modelBuilder.Entity<AttackScenario>().ToTable("AttackScenarios");
        modelBuilder.Entity<SimulationBatch>().ToTable("SimulationBatches");
        modelBuilder.Entity<SimulationRun>().ToTable("SimulationRuns");
        modelBuilder.Entity<SimulationEvent>().ToTable("SimulationEvents");
        modelBuilder.Entity<ResilienceScoreDetail>().ToTable("ResilienceScores");
        modelBuilder.Entity<BatchStatistics>().ToTable("BatchStatistics");
        modelBuilder.Entity<AiProviderSetting>().ToTable("AiProviderSettings");

        modelBuilder.Entity<TwinEdge>()
            .HasOne(e => e.FromNode)
            .WithMany()
            .HasForeignKey(e => e.FromNodeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TwinEdge>()
            .HasOne(e => e.ToNode)
            .WithMany()
            .HasForeignKey(e => e.ToNodeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SimulationRun>()
            .HasOne(r => r.ScoreDetail)
            .WithOne(s => s.Run)
            .HasForeignKey<ResilienceScoreDetail>(s => s.RunId);
    }
}
