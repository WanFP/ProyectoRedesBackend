using Microsoft.EntityFrameworkCore;
using ContaminaDOSApi.Models;

namespace ContaminaDOSApi.data
{
    public class ContaminaDosDb:DbContext
    {
        public ContaminaDosDb(DbContextOptions<ContaminaDosDb> options) : base(options) { }

        public DbSet<JuegoContaminaDOS> Juegos { get; set; }
        public DbSet<Jugador> Jugadores { get; set; }
        public DbSet<Ronda> Rondas { get; set; }
        public DbSet<Voto> Votos { get; set; }
        public DbSet<Accion> Acciones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración de JuegoContaminaDOS
        modelBuilder.Entity<JuegoContaminaDOS>()
            .HasMany(j => j.Jugadores)
            .WithOne(j => j.Juego)
            .HasForeignKey(j => j.JuegoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JuegoContaminaDOS>()
            .HasMany(j => j.Rondas)
            .WithOne(r => r.Juego)
            .HasForeignKey(r => r.JuegoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configuración de Jugador
        modelBuilder.Entity<Jugador>()
            .HasOne(j => j.Juego)
            .WithMany(j => j.Jugadores)
            .HasForeignKey(j => j.JuegoId);

        // Configuración de Ronda
        modelBuilder.Entity<Ronda>()
            .HasOne(r => r.Juego)
            .WithMany(j => j.Rondas)
            .HasForeignKey(r => r.JuegoId);

        // Configuración de Voto
        modelBuilder.Entity<Voto>()
            .HasOne(v => v.Ronda)
            .WithMany(r => r.Votos)
            .HasForeignKey(v => v.RondaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configuración de Accion -> Ronda
        modelBuilder.Entity<Accion>()
            .HasOne(a => a.Ronda)
            .WithMany(r => r.Acciones)
            .HasForeignKey(a => a.RondaId)
            .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
