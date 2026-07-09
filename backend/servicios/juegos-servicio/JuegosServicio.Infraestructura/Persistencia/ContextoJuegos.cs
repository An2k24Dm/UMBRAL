using JuegosServicio.Infraestructura.Persistencia.Modelos;
using Microsoft.EntityFrameworkCore;

namespace JuegosServicio.Infraestructura.Persistencia;

public sealed class ContextoJuegos : DbContext
{
    public ContextoJuegos(DbContextOptions<ContextoJuegos> opciones) : base(opciones) { }

    public DbSet<TriviaModelo> Trivias => Set<TriviaModelo>();
    public DbSet<PreguntaModelo> Preguntas => Set<PreguntaModelo>();
    public DbSet<OpcionModelo> Opciones => Set<OpcionModelo>();
    public DbSet<EventoSalidaModelo> EventosSalida => Set<EventoSalidaModelo>();
    public DbSet<BusquedaTesoroModelo> BusquedasTesoro => Set<BusquedaTesoroModelo>();
    public DbSet<PistaModelo> Pistas => Set<PistaModelo>();
    public DbSet<MisionModelo> Misiones => Set<MisionModelo>();
    public DbSet<EtapaModelo> Etapas => Set<EtapaModelo>();

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
            e.Property(x => x.TiempoEstimado).HasColumnName("tiempo_estimado").IsRequired().HasDefaultValue(10);
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

        // ---------- EventoSalida (Outbox) ----------
        constructor.Entity<EventoSalidaModelo>(e =>
        {
            e.ToTable("EventoSalida");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(100).IsRequired();
            e.Property(x => x.Datos).HasColumnName("datos").IsRequired();
            e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion").IsRequired();
            e.Property(x => x.Procesado).HasColumnName("procesado").IsRequired();
        });

        // ---------- BusquedaTesoro ----------
        constructor.Entity<BusquedaTesoroModelo>(e =>
        {
            e.ToTable("BusquedaTesoro");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(200).IsRequired();
            e.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(1000).IsRequired();
            e.Property(x => x.CreadorId).HasColumnName("creador_id").IsRequired();
            e.Property(x => x.Estado).HasColumnName("estado").IsRequired();
            e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion").IsRequired();
            e.Property(x => x.Tiempo).HasColumnName("tiempo").IsRequired().HasDefaultValue(0);
            e.Property(x => x.Puntaje).HasColumnName("puntaje").IsRequired().HasDefaultValue(0);
            e.Property(x => x.CodigoQr).HasColumnName("codigo_qr").HasMaxLength(32).IsRequired().HasDefaultValue("");
            e.HasIndex(x => x.Nombre).IsUnique();
            e.HasMany(x => x.Pistas)
                .WithOne(p => p.BusquedaTesoro)
                .HasForeignKey(p => p.BusquedaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Pista ----------
        constructor.Entity<PistaModelo>(e =>
        {
            e.ToTable("Pista");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.BusquedaId).HasColumnName("busqueda_id").IsRequired();
            e.Property(x => x.Contenido).HasColumnName("contenido").HasMaxLength(1000).IsRequired();
        });

        // ---------- Mision ----------
        constructor.Entity<MisionModelo>(e =>
        {
            e.ToTable("Mision");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(200).IsRequired();
            e.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(1000).IsRequired();
            e.Property(x => x.CreadorId).HasColumnName("creador_id").IsRequired();
            e.Property(x => x.Estado).HasColumnName("estado").IsRequired();
            e.Property(x => x.Dificultad).HasColumnName("dificultad").IsRequired().HasDefaultValue(1);
            e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion").IsRequired();
            e.HasIndex(x => x.Nombre).IsUnique();
            e.HasMany(x => x.Etapas)
                .WithOne(et => et.Mision)
                .HasForeignKey(et => et.MisionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Etapa ----------
        constructor.Entity<EtapaModelo>(e =>
        {
            e.ToTable("Etapa");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.MisionId).HasColumnName("mision_id").IsRequired();
            e.Property(x => x.Orden).HasColumnName("orden").IsRequired();
            e.Property(x => x.TipoModoDeJuego).HasColumnName("tipo_modo_de_juego").IsRequired();
            e.Property(x => x.ModoDeJuegoId).HasColumnName("modo_de_juego_id").IsRequired();
            e.HasIndex(x => new { x.MisionId, x.Orden });
        });
    }
}
