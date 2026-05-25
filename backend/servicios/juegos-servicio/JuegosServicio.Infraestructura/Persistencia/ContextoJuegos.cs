using JuegosServicio.Infraestructura.Persistencia.Modelos;
using Microsoft.EntityFrameworkCore;

namespace JuegosServicio.Infraestructura.Persistencia;

public sealed class ContextoJuegos : DbContext
{
    public ContextoJuegos(DbContextOptions<ContextoJuegos> opciones) : base(opciones) { }

    public DbSet<TriviaModelo> Trivias => Set<TriviaModelo>();
    public DbSet<PreguntaModelo> Preguntas => Set<PreguntaModelo>();
    public DbSet<OpcionModelo> Opciones => Set<OpcionModelo>();

    protected override void OnModelCreating(ModelBuilder constructor)
    {
        constructor.HasDefaultSchema("juegos");

        // ---------- Trivia ----------
        constructor.Entity<TriviaModelo>(e =>
        {
            e.ToTable("Trivia");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(200).IsRequired();
            e.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(1000).IsRequired();
            e.Property(x => x.CreadorId).HasColumnName("creador_id").IsRequired();
            e.Property(x => x.TiempoLimitePorPregunta).HasColumnName("tiempo_limite_por_pregunta").IsRequired();
            e.Property(x => x.Estado).HasColumnName("estado").IsRequired();
            e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion").IsRequired();

            e.HasIndex(x => x.Nombre).IsUnique();

            e.HasMany(x => x.Preguntas)
                .WithOne(p => p.Trivia)
                .HasForeignKey(p => p.TriviaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Pregunta ----------
        constructor.Entity<PreguntaModelo>(e =>
        {
            e.ToTable("Pregunta");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TriviaId).HasColumnName("trivia_id").IsRequired();
            e.Property(x => x.Enunciado).HasColumnName("enunciado").HasMaxLength(500).IsRequired();
            e.Property(x => x.PuntajeAsignado).HasColumnName("puntaje_asignado").IsRequired();

            e.HasMany(x => x.Opciones)
                .WithOne(o => o.Pregunta)
                .HasForeignKey(o => o.PreguntaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Opcion ----------
        constructor.Entity<OpcionModelo>(e =>
        {
            e.ToTable("Opcion");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.PreguntaId).HasColumnName("pregunta_id").IsRequired();
            e.Property(x => x.Texto).HasColumnName("texto").HasMaxLength(300).IsRequired();
            e.Property(x => x.EsCorrecta).HasColumnName("es_correcta").IsRequired();
        });
    }
}
