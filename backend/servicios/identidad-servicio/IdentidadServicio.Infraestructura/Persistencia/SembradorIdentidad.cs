using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Dominio.Enums;
using Microsoft.EntityFrameworkCore;

namespace IdentidadServicio.Infraestructura.Persistencia;

public static class SembradorIdentidad
{
    private static readonly Guid IdAdmin = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private const string IdKeycloakAdmin = "11111111-1111-1111-1111-111111111111";

    public static async Task SembrarAsync(
        ContextoIdentidad contexto,
        IProveedorFechaHora reloj,
        CancellationToken cancelacion)
    {
        await EjecutarMigracionConReintentosAsync(contexto, cancelacion);

        var ahora = reloj.ObtenerFechaHoraUtc();
        var nacimientoAdmin = new DateTime(1995, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        await SembrarAdministradorAsync(
            contexto,
            IdAdmin,
            "administrador01",
            IdKeycloakAdmin,
            "Angelo",
            "Di Martino",
            "administrador01@umbral.com",
            SexoPersona.Masculino,
            nacimientoAdmin,
            "AD-001",
            ahora,
            cancelacion);

        await contexto.SaveChangesAsync(cancelacion);
    }

    private static async Task SembrarAdministradorAsync(
        ContextoIdentidad contexto,
        Guid usuarioId,
        string nombreUsuario,
        string idKeycloak,
        string nombre,
        string apellido,
        string correo,
        SexoPersona sexo,
        DateTime fechaNacimiento,
        string codigoAdministrador,
        DateTime ahora,
        CancellationToken cancelacion)
    {
        var usuario = await ObtenerOCrearUsuarioAsync(
            contexto,
            usuarioId,
            nombreUsuario,
            idKeycloak,
            RolUsuario.Administrador,
            EstadoUsuario.Activo,
            ahora,
            cancelacion);

        var persona = await ObtenerOCrearPersonaAsync(
            contexto,
            usuario.Id,
            nombre,
            apellido,
            correo,
            sexo,
            fechaNacimiento,
            ahora,
            cancelacion);

        var existeAdministrador = await contexto.Administradores
            .AnyAsync(a => a.PersonaId == persona.Id, cancelacion);

        if (!existeAdministrador)
        {
            contexto.Administradores.Add(new AdministradorModelo
            {
                Id = Guid.NewGuid(),
                PersonaId = persona.Id,
                CodigoAdministrador = codigoAdministrador,
                FechaRegistro = ahora
            });
        }
    }

    private static async Task<UsuarioModelo> ObtenerOCrearUsuarioAsync(
        ContextoIdentidad contexto,
        Guid id,
        string nombreUsuario,
        string idKeycloak,
        RolUsuario rol,
        EstadoUsuario estado,
        DateTime ahora,
        CancellationToken cancelacion)
    {
        var usuario = await contexto.Usuarios
            .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario, cancelacion);

        if (usuario is not null)
        {
            usuario.IdKeycloak = idKeycloak;
            usuario.Rol = (int)rol;
            usuario.Estado = (int)estado;
            return usuario;
        }

        usuario = new UsuarioModelo
        {
            Id = id,
            NombreUsuario = nombreUsuario,
            IdKeycloak = idKeycloak,
            Rol = (int)rol,
            Estado = (int)estado,
            FechaRegistro = ahora
        };

        contexto.Usuarios.Add(usuario);

        return usuario;
    }

    private static async Task<PersonaModelo> ObtenerOCrearPersonaAsync(
        ContextoIdentidad contexto,
        Guid usuarioId,
        string nombre,
        string apellido,
        string correo,
        SexoPersona sexo,
        DateTime fechaNacimiento,
        DateTime ahora,
        CancellationToken cancelacion)
    {
        // Dirección y teléfono son necesarios para reconstruir DatosContacto
        // desde persistencia hacia el dominio.
        var (direccion, telefono) = ObtenerContactoSembrado(usuarioId);

        var persona = await contexto.Personas
            .FirstOrDefaultAsync(p => p.UsuarioId == usuarioId, cancelacion);

        if (persona is not null)
        {
            persona.Nombre = nombre;
            persona.Apellido = apellido;
            persona.Correo = correo;
            persona.Direccion ??= direccion;
            persona.Telefono ??= telefono;
            persona.Sexo = (int)sexo;
            persona.FechaNacimiento = fechaNacimiento;
            return persona;
        }

        persona = new PersonaModelo
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Nombre = nombre,
            Apellido = apellido,
            Correo = correo,
            Direccion = direccion,
            Telefono = telefono,
            Sexo = (int)sexo,
            FechaNacimiento = fechaNacimiento,
            FechaRegistro = ahora
        };

        contexto.Personas.Add(persona);

        return persona;
    }

    private static (string Direccion, string Telefono) ObtenerContactoSembrado(Guid usuarioId)
    {
        if (usuarioId == IdAdmin)
            return ("Av. Bolívar, Caracas", "04141000001");

        return ("Caracas, Venezuela", "04141000099");
    }

    private static async Task EjecutarMigracionConReintentosAsync(
        ContextoIdentidad contexto,
        CancellationToken cancelacion)
    {
        const int intentosMaximos = 15;
        var espera = TimeSpan.FromSeconds(3);

        for (var intento = 1; intento <= intentosMaximos; intento++)
        {
            try
            {
                await contexto.Database.MigrateAsync(cancelacion);
                return;
            }
            catch when (intento < intentosMaximos)
            {
                await Task.Delay(espera, cancelacion);
            }
        }
    }
}