using Microsoft.EntityFrameworkCore;
using TadbirRuleEngine.Api.Models;

namespace TadbirRuleEngine.Api.Data;

public class TadbirDbContext : DbContext
{
    public TadbirDbContext(DbContextOptions<TadbirDbContext> options) : base(options)
    {
    }

    public DbSet<SwaggerSource> SwaggerSources { get; set; }
    public DbSet<ApiDefinition> ApiDefinitions { get; set; }
    public DbSet<Policy> Policies { get; set; }
    public DbSet<Rule> Rules { get; set; }
    public DbSet<PolicySchedule> PolicySchedules { get; set; }
    public DbSet<ExecutionLog> ExecutionLogs { get; set; }
    public DbSet<AuthenticationSetting> AuthenticationSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SwaggerSource
        modelBuilder.Entity<SwaggerSource>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // ApiDefinition
        modelBuilder.Entity<ApiDefinition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.SwaggerSource)
                  .WithMany(e => e.ApiDefinitions)
                  .HasForeignKey(e => e.SwaggerSourceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Policy
        modelBuilder.Entity<Policy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasOne(e => e.AuthenticationSetting)
                  .WithMany()
                  .HasForeignKey(e => e.AuthenticationSettingId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Rule
        modelBuilder.Entity<Rule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Policy)
                  .WithMany(e => e.Rules)
                  .HasForeignKey(e => e.PolicyId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ApiDefinition)
                  .WithMany(e => e.Rules)
                  .HasForeignKey(e => e.ApiDefinitionId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // PolicySchedule
        modelBuilder.Entity<PolicySchedule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Policy)
                  .WithMany(e => e.Schedules)
                  .HasForeignKey(e => e.PolicyId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ExecutionLog
        modelBuilder.Entity<ExecutionLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Policy)
                  .WithMany(e => e.ExecutionLogs)
                  .HasForeignKey(e => e.PolicyId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Rule)
                  .WithMany()
                  .HasForeignKey(e => e.RuleId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // AuthenticationSetting
        modelBuilder.Entity<AuthenticationSetting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
        });
    }
}