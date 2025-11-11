using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using OrquestadorGeoPredio.Entities; 

namespace OrquestadorGeoPredio.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Esta propiedad representa la tabla CR_TERRENO
        public DbSet<CrTerrenoEntity> CrTerreno { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración extra si queremos usar tipos espaciales más adelante
            modelBuilder.Entity<CrTerrenoEntity>()
                .Property(e => e.Shape)
                .HasColumnType("geometry"); // opcional
        }
    }
}
