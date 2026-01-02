using LogiHub.Infrastructure;
using LogiHub.Models.Shared;
using Microsoft.EntityFrameworkCore;

namespace LogiHub.Services;

public class TemplateDbContext : DbContext
{
    public TemplateDbContext()
    {
    }

    public TemplateDbContext(DbContextOptions<TemplateDbContext> options) : base(options)
    {
        DataGenerator.Initialize(this);
    }

    public DbSet<User> Users { get; set; }
    public DbSet<AziendaEsterna> AziendeEsterne { get; set; }
    public DbSet<SemiLavorato> SemiLavorati { get; set; }
    public DbSet<Ubicazione> Ubicazioni { get; set; }
    public DbSet<Azione> Azioni { get; set; }
    public DbSet<SessioneInventario> SessioniInventario { get; set; }
    public DbSet<RigaInventario> RigheInventario { get; set; }
    public DbSet<SessioneUbicazione> SessioniUbicazioni { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Azione>()
            .HasOne(a => a.SemiLavorato)
            .WithMany(s => s.Azioni)
            .HasForeignKey(a => a.SemiLavoratoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SemiLavorato>()
            .HasIndex(s => s.Id)
            .IsUnique();

        modelBuilder.Entity<SemiLavorato>()
            .HasQueryFilter(s => !s.Eliminato);

        modelBuilder.Entity<RigaInventario>(entity =>
        {
            entity.HasOne(r => r.UbicazionePrevista)
                .WithMany()
                .HasForeignKey(r => r.UbicazionePrevistaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.UbicazioneReale)
                .WithMany()
                .HasForeignKey(r => r.UbicazioneRealeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<SessioneUbicazione>(entity =>
        {
            entity.HasIndex(x => new { x.SessioneInventarioId, x.UbicazioneId }).IsUnique();

            entity.HasOne(x => x.Ubicazione)
                .WithMany()
                .HasForeignKey(x => x.UbicazioneId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}