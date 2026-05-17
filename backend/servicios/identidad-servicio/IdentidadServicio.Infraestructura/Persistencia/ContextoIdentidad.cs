using Microsoft.EntityFrameworkCore;

namespace IdentidadServicio.Infraestructura.Persistencia;

public sealed class ContextoIdentidad : DbContext
{
    public ContextoIdentidad(DbContextOptions<ContextoIdentidad> opciones) : base(opciones) { }

    public DbSet<UsuarioModelo> Usuarios => Set<UsuarioModelo>();
    public DbSet<PersonaModelo> Personas => Set<PersonaModelo>();
    public DbSet<AdministradorModelo> Administradores => Set<AdministradorModelo>();
    public DbSet<OperadorModelo> Operadores => Set<OperadorModelo>();
    public DbSet<ParticipanteModelo> Participantes => Set<ParticipanteModelo>();

    protected override void OnModelCreating(ModelBuilder constructor)
    {
        constructor.HasDefaultSchema("identidad");

        // ---------- Usuario ----------
        constructor.Entity<UsuarioModelo>(e =>
        {
            e.ToTable("Usuario");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.NombreUsuario).HasColumnName("nombre_usuario").HasMaxLength(50).IsRequired();
            e.Property(x => x.IdKeycloak).HasColumnName("id_keycloak").HasMaxLength(64).IsRequired();
            e.Property(x => x.Rol).HasColumnName("rol").IsRequired();
            e.Property(x => x.Estado).HasColumnName("estado").IsRequired();
            e.Property(x => x.FechaRegistro).HasColumnName("fecha_registro").IsRequired();

            e.HasIndex(x => x.NombreUsuario).IsUnique();
            e.HasIndex(x => x.IdKeycloak).IsUnique();

            e.HasOne(x => x.Persona)
                .WithOne(p => p.Usuario)
                .HasForeignKey<PersonaModelo>(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Persona ----------
        constructor.Entity<PersonaModelo>(e =>
        {
            e.ToTable("Persona");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UsuarioId).HasColumnName("usuario_id").IsRequired();
            e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();
            e.Property(x => x.Apellido).HasColumnName("apellido").HasMaxLength(100).IsRequired();
            e.Property(x => x.Correo).HasColumnName("correo").HasMaxLength(200).IsRequired();
            e.Property(x => x.Direccion).HasColumnName("direccion").HasMaxLength(250);
            e.Property(x => x.Telefono).HasColumnName("telefono").HasMaxLength(30);
            e.Property(x => x.Sexo).HasColumnName("sexo").IsRequired();
            e.Property(x => x.FechaNacimiento).HasColumnName("fecha_nacimiento").IsRequired();
            e.Property(x => x.FechaRegistro).HasColumnName("fecha_registro").IsRequired();

            e.HasIndex(x => x.UsuarioId).IsUnique();
            e.HasIndex(x => x.Correo).IsUnique();

            e.HasOne(x => x.Administrador)
                .WithOne(a => a.Persona)
                .HasForeignKey<AdministradorModelo>(a => a.PersonaId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Operador)
                .WithOne(o => o.Persona)
                .HasForeignKey<OperadorModelo>(o => o.PersonaId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Participante)
                .WithOne(p => p.Persona)
                .HasForeignKey<ParticipanteModelo>(p => p.PersonaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Administrador ----------
        constructor.Entity<AdministradorModelo>(e =>
        {
            e.ToTable("Administrador");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.PersonaId).HasColumnName("persona_id").IsRequired();
            e.Property(x => x.CodigoAdministrador).HasColumnName("codigo_administrador").HasMaxLength(50);
            e.Property(x => x.FechaRegistro).HasColumnName("fecha_registro").IsRequired();
            e.HasIndex(x => x.PersonaId).IsUnique();
        });

        // ---------- Operador ----------
        constructor.Entity<OperadorModelo>(e =>
        {
            e.ToTable("Operador");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.PersonaId).HasColumnName("persona_id").IsRequired();
            e.Property(x => x.CodigoOperador).HasColumnName("codigo_operador").HasMaxLength(50).IsRequired();
            e.Property(x => x.FechaRegistro).HasColumnName("fecha_registro").IsRequired();
            e.HasIndex(x => x.PersonaId).IsUnique();
            e.HasIndex(x => x.CodigoOperador).IsUnique();
        });

        // ---------- Participante ----------
        constructor.Entity<ParticipanteModelo>(e =>
        {
            e.ToTable("Participante");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.PersonaId).HasColumnName("persona_id").IsRequired();
            e.Property(x => x.Alias).HasColumnName("alias").HasMaxLength(50).IsRequired();
            e.Property(x => x.FechaRegistro).HasColumnName("fecha_registro").IsRequired();
            e.HasIndex(x => x.PersonaId).IsUnique();
            e.HasIndex(x => x.Alias).IsUnique();
        });
    }
}
