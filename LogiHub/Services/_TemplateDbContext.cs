using LogiHub.Infrastructure;
using LogiHub.Services.Shared;
using Microsoft.EntityFrameworkCore;

namespace LogiHub.Services
{
    public class TemplateDbContext : DbContext
    {
        public TemplateDbContext()
        {
        }

        public TemplateDbContext(DbContextOptions<TemplateDbContext> options) : base(options)
        {
            DataGenerator.InitializeUsers(this);
        }

        public DbSet<User> Users { get; set; }
        public DbSet<AziendaEsterna> AziendeEsterne { get; set; }
        public DbSet<SemiLavorato> SemiLavorati { get; set; }
        public DbSet<Ubicazione> Ubicazioni { get; set; }
        public DbSet<Azione> Azioni { get; set; }

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
               .HasQueryFilter(s => !s.IsDeleted);
        }
    }
}
