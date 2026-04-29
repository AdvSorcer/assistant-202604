using Assistant.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Assistant.Api.Infrastructure;

public sealed class AssistantDbContext(DbContextOptions<AssistantDbContext> options) : DbContext(options)
{
    public DbSet<ManagedVm> Vms => Set<ManagedVm>();
    public DbSet<VmAccount> VmAccounts => Set<VmAccount>();
    public DbSet<VmUrl> VmUrls => Set<VmUrl>();
    public DbSet<DailyLog> DailyLogs => Set<DailyLog>();
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();
    public DbSet<WikiPage> WikiPages => Set<WikiPage>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ManagedVm>(entity =>
        {
            entity.Property(vm => vm.Name).HasMaxLength(120);
            entity.Property(vm => vm.Hostname).HasMaxLength(255);
            entity.Property(vm => vm.IpAddress).HasMaxLength(80);
            entity.HasIndex(vm => vm.IsFavorite);
            entity.HasMany(vm => vm.Accounts).WithOne().HasForeignKey(account => account.ManagedVmId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(vm => vm.Urls).WithOne().HasForeignKey(url => url.ManagedVmId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VmAccount>(entity =>
        {
            entity.Property(account => account.Label).HasMaxLength(80);
            entity.Property(account => account.Username).HasMaxLength(120);
        });

        modelBuilder.Entity<VmUrl>(entity =>
        {
            entity.Property(url => url.Label).HasMaxLength(80);
            entity.Property(url => url.Url).HasMaxLength(2048);
        });

        modelBuilder.Entity<DailyLog>(entity =>
        {
            entity.HasIndex(log => log.Date).IsUnique();
        });

        modelBuilder.Entity<TodoItem>(entity =>
        {
            entity.Property(todo => todo.Title).HasMaxLength(200);
            entity.Property(todo => todo.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(todo => todo.DueDate);
        });

        modelBuilder.Entity<WikiPage>(entity =>
        {
            entity.Property(page => page.Title).HasMaxLength(200);
            entity.Property(page => page.Slug).HasMaxLength(220);
            entity.HasIndex(page => page.IsPinned);
            entity.HasIndex(page => page.Slug).IsUnique();
        });

        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.Property(setting => setting.Key).HasMaxLength(120);
            entity.HasIndex(setting => setting.Key).IsUnique();
        });
    }
}
